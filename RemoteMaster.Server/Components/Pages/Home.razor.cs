// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class Home
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private OrganizationalUnit? _selectedNode;
    private List<TreeItemData<object>> _treeItems = [];

    private readonly List<ComputerDto> _selectedComputers = [];
    private readonly ConcurrentDictionary<string, ComputerDto> _availableComputers = new();
    private readonly ConcurrentDictionary<string, ComputerDto> _unavailableComputers = new();
    private readonly ConcurrentDictionary<string, ComputerDto> _pendingComputers = new();

    private ClaimsPrincipal? _user;
    private ApplicationUser? _currentUser;

    private IDictionary<NotificationMessage, bool>? _messages;

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;

        _user = authState.User;

        var userId = _user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        _currentUser = await UserManager.Users
            .Include(u => u.UserOrganizations)
            .ThenInclude(uo => uo.Organization)
            .Include(u => u.UserOrganizationalUnits)
            .ThenInclude(uou => uou.OrganizationalUnit)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (_currentUser == null)
        {
            Log.Warning("User not found in database.");

            return;
        }

        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_currentUser == null)
        {
            Log.Warning("Current user not found");
            
            return;
        }

        var nodes = await LoadNodes();
        _treeItems = nodes.Select(node => new UnifiedTreeItemData(node)).Cast<TreeItemData<object>>().ToList();

        var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(_currentUser.Id);
        
        if (!accessTokenResult.IsSuccess)
        {
            Log.Error("Failed to retrieve access token for user {UserId}", _currentUser.Id);
            
            return;
        }

        _messages = await NotificationService.GetNotifications();
    }

    private bool DrawerOpen { get; set; }

    private async Task<IEnumerable<object>> LoadNodes(Guid? organizationId = null, Guid? parentId = null)
    {
        if (_currentUser == null)
        {
            return [];
        }

        var accessibleOrganizationIds = _currentUser.UserOrganizations
            .Select(uo => uo.OrganizationId)
            .ToList();

        var accessibleOrganizationalUnitIds = _currentUser.UserOrganizationalUnits
            .Select(uou => uou.OrganizationalUnitId)
            .ToList();

        var units = new List<object>();

        if (organizationId != null)
        {
            return units;
        }

        var organizations = await OrganizationRepository.GetOrganizationsWithAccessibleUnitsAsync(accessibleOrganizationIds, accessibleOrganizationalUnitIds);

        units.AddRange(organizations);

        return units;
    }

    private void ToggleDrawer() => DrawerOpen = !DrawerOpen;

    [Authorize(Roles = "Administrator")]
    private async Task OpenHostConfig()
    {
        var dialogOptions = new DialogOptions
        {
            CloseOnEscapeKey = true
        };

        await DialogService.ShowAsync<HostConfigurationGenerator>("Host Configuration Generator", dialogOptions);
    }

    [Authorize(Roles = "Administrator")]
    private void OpenCertificateRenewTasks() => NavigationManager.NavigateTo("/certificates/tasks");


    [Authorize(Roles = "Administrator")]
    private async Task PublishCrl()
    {
        var crlResult = await CrlService.GenerateCrlAsync();

        if (!crlResult.IsSuccess)
        {
            Log.Error("Failed to generate CRL: {Message}", crlResult.Errors.FirstOrDefault()?.Message);
            Snackbar.Add("Failed to generate CRL", Severity.Error);
            
            return;
        }

        var publishResult = await CrlService.PublishCrlAsync(crlResult.Value);

        if (publishResult.IsSuccess)
        {
            Snackbar.Add("CRL successfully published", Severity.Success);
        }
        else
        {
            Log.Error("Failed to publish CRL: {Message}", publishResult.Errors.FirstOrDefault()?.Message);
            Snackbar.Add("Failed to publish CRL", Severity.Error);
        }
    }

    private void ManageProfile() => NavigationManager.NavigateTo("/Account/Manage");

    private void Logout() => NavigationManager.NavigateTo("/Account/Logout");

    private async Task OnNodeSelected(object? node)
    {
        _selectedComputers.Clear();
        _availableComputers.Clear();
        _unavailableComputers.Clear();
        _pendingComputers.Clear();

        switch (node)
        {
            case Organization:
                break;
            case OrganizationalUnit orgUnit:
                _selectedNode = orgUnit;
                await LoadComputers(orgUnit);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadComputers(OrganizationalUnit orgUnit)
    {
        var computers = orgUnit.Computers.ToList();

        var newPendingComputers = new ConcurrentDictionary<string, ComputerDto>();

        foreach (var computer in computers.Where(computer => !_availableComputers.ContainsKey(computer.IpAddress) && !_unavailableComputers.ContainsKey(computer.IpAddress)))
        {
            var computerDto = new ComputerDto(computer.Name, computer.IpAddress, computer.MacAddress)
            {
                Id = computer.Id,
                OrganizationId = computer.Parent.OrganizationId,
                OrganizationalUnitId = computer.ParentId,
                Thumbnail = null
            };

            newPendingComputers.TryAdd(computerDto.IpAddress, computerDto);
        }

        _pendingComputers.Clear();

        foreach (var kvp in newPendingComputers)
        {
            _pendingComputers.TryAdd(kvp.Key, kvp.Value);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LogonComputers()
    {
        var channel = Channel.CreateUnbounded<ComputerDto>();

        var logonTasks = _selectedComputers.Select(async computer =>
        {
            await LogonComputer(computer);
            await channel.Writer.WriteAsync(computer);
        });

        var readTask = Task.Run(async () =>
        {
            await foreach (var _ in channel.Reader.ReadAllAsync())
            {
                await InvokeAsync(StateHasChanged);
            }
        });

        await Task.WhenAll(logonTasks);
        channel.Writer.Complete();
        await readTask;

        ResetSelections();
    }

    private async Task LogonComputer(ComputerDto computerDto)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var cancellationToken = cts.Token;

        try
        {
            const string url = "hubs/control?thumbnail=true";

            var connection = await SetupConnection(computerDto, url, true, cancellationToken);

            connection.On<byte[]>("ReceiveThumbnail", async thumbnailBytes =>
            {
                if (thumbnailBytes.Length > 0)
                {
                    computerDto.Thumbnail = thumbnailBytes;

                    await MoveToAvailable(computerDto);
                }
                else
                {
                    await MoveToUnavailable(computerDto);
                }

                await InvokeAsync(StateHasChanged);
            });

            connection.On("ReceiveCloseConnection", async () =>
            {
                await connection.StopAsync(cancellationToken);

                Log.Information("Connection closed for {IPAddress}", computerDto.IpAddress);
            });

            await connection.StartAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await MoveToUnavailable(computerDto);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Log.Error("Exception in LogonComputer for {IPAddress}: {Message}", computerDto.IpAddress, ex.Message);

            await MoveToUnavailable(computerDto);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LogoffComputers()
    {
        var tasks = _selectedComputers
            .Where(c => _availableComputers.ContainsKey(c.IpAddress) || _unavailableComputers.ContainsKey(c.IpAddress))
            .Select(LogoffComputer);

        await Task.WhenAll(tasks);

        ResetSelections();
    }

    private async Task LogoffComputer(ComputerDto computerDto)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var cancellationToken = cts.Token;

        try
        {
            var connection = await SetupConnection(computerDto, "hubs/control", false, cancellationToken);

            connection.On("ReceiveCloseConnection", async () =>
            {
                await connection.StopAsync(cancellationToken);

                Log.Information("Connection closed for {IPAddress}", computerDto.IpAddress);
            });

            computerDto.Thumbnail = null;

            await MoveToPending(computerDto);
        }
        catch (Exception ex)
        {
            Log.Error("Exception in LogoffComputer for {IPAddress}: {Message}", computerDto.IpAddress, ex.Message);
        }
    }

    private async Task MoveToAvailable(ComputerDto computerDto)
    {
        if (_pendingComputers.ContainsKey(computerDto.IpAddress))
        {
            _pendingComputers.TryRemove(computerDto.IpAddress, out _);
        }
        else if (_unavailableComputers.ContainsKey(computerDto.IpAddress))
        {
            _unavailableComputers.TryRemove(computerDto.IpAddress, out _);
        }

        _availableComputers.TryAdd(computerDto.IpAddress, computerDto);

        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveToUnavailable(ComputerDto computerDto)
    {
        computerDto.Thumbnail = null;

        if (_pendingComputers.ContainsKey(computerDto.IpAddress))
        {
            _pendingComputers.TryRemove(computerDto.IpAddress, out _);
        }
        else if (_availableComputers.ContainsKey(computerDto.IpAddress))
        {
            _availableComputers.TryRemove(computerDto.IpAddress, out _);
        }

        _unavailableComputers.TryAdd(computerDto.IpAddress, computerDto);

        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveToPending(ComputerDto computerDto)
    {
        computerDto.Thumbnail = null;

        if (_availableComputers.ContainsKey(computerDto.IpAddress))
        {
            _availableComputers.TryRemove(computerDto.IpAddress, out _);
        }
        else if (_unavailableComputers.ContainsKey(computerDto.IpAddress))
        {
            _unavailableComputers.TryRemove(computerDto.IpAddress, out _);
        }

        _pendingComputers.TryAdd(computerDto.IpAddress, computerDto);

        await InvokeAsync(StateHasChanged);
    }

    private async Task<HubConnection> SetupConnection(ComputerDto computerDto, string hubPath, bool startConnection, CancellationToken cancellationToken)
    {
        var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{computerDto.IpAddress}:5001/{hubPath}", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                    return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                };
            })
            .AddMessagePackProtocol()
            .Build();

        if (!startConnection)
        {
            return connection;
        }

        await connection.StartAsync(cancellationToken);

        Log.Information("Connection started for {IPAddress}", computerDto.IpAddress);

        return connection;
    }

    private void SelectAllPendingComputers()
    {
        foreach (var computer in _pendingComputers.Values)
        {
            SelectComputer(computer, true);
        }
    }

    private void DeselectAllPendingComputers()
    {
        foreach (var computer in _pendingComputers.Values)
        {
            SelectComputer(computer, false);
        }
    }

    private void SelectAllAvailableComputers()
    {
        foreach (var computer in _availableComputers.Values)
        {
            SelectComputer(computer, true);
        }
    }

    private void DeselectAllAvailableComputers()
    {
        foreach (var computer in _availableComputers.Values)
        {
            SelectComputer(computer, false);
        }
    }

    private void SelectAllUnavailableComputers()
    {
        foreach (var computer in _unavailableComputers.Values)
        {
            SelectComputer(computer, true);
        }
    }

    private void DeselectAllUnavailableComputers()
    {
        foreach (var computer in _unavailableComputers.Values)
        {
            SelectComputer(computer, false);
        }
    }

    private void SelectComputer(ComputerDto computerDto, bool isSelected)
    {
        if (isSelected)
        {
            if (!_selectedComputers.Contains(computerDto))
            {
                _selectedComputers.Add(computerDto);
            }
        }
        else
        {
            _selectedComputers.Remove(computerDto);
        }

        InvokeAsync(StateHasChanged);
    }

    private async Task ExecuteDialog<TDialog>(string title, DialogParameters? parameters = null, DialogOptions? options = null) where TDialog : ComponentBase
    {
        var parametersWithAdditional = parameters ?? [];

        if (parametersWithAdditional.TryGet<Dictionary<string, object>>(nameof(CommonDialogWrapper<TDialog>.AdditionalParameters)) == null)
        {
            parametersWithAdditional.Add(nameof(CommonDialogWrapper<TDialog>.AdditionalParameters), new Dictionary<string, object>());
        }

        await DialogService.ShowAsync<CommonDialogWrapper<TDialog>>(title, parametersWithAdditional, options);
    }

    private async Task Power() => await ExecuteAction<PowerDialog>("Power");

    private async Task WakeUp() => await ExecuteAction<WakeUpDialog>("Wake Up", false, false, requireConnections: false);

    private async Task Connect()
    {
        if (_user == null)
        {
            throw new InvalidOperationException("User is not initialized.");
        }

        if (_user.IsInRole("Viewer"))
        {
            await ConnectAsViewer();
        }
        else
        {
            await ExecuteAction<ConnectDialog>("Connect");
        }
    }

    private async Task ConnectAsViewer()
    {
        var computers = _selectedComputers.Where(c => _availableComputers.ContainsKey(c.IpAddress)).ToList();

        foreach (var computer in computers)
        {
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");
            await module.InvokeVoidAsync("openNewWindow", $"/{computer.IpAddress}/access?frameRate=60&imageQuality=25&cursorTracking=true&inputEnabled=false", 600, 400);
        }
    }

    private async Task Lock() => await ExecuteAction<LockWorkStationDialog>("Lock Workstation");

    private async Task OpenShell() => await ExecuteAction<OpenShellDialog>("Open Shell", false, false, requireConnections: false);

    private async Task ExecuteScript() => await ExecuteAction<ScriptExecutorDialog>("Execute Script");

    private async Task ManagePsExecRules() => await ExecuteAction<PsExecRulesDialog>("PSExec Rules", hubPath: "hubs/service");

    private async Task SetMonitorState() => await ExecuteAction<MonitorStateDialog>("Set Monitor State");

    private async Task ScreenRecorder() => await ExecuteAction<ScreenRecorderDialog>("Screen Recorder", hubPath: "hubs/screenrecorder");

    private async Task DomainMembership() => await ExecuteAction<DomainMembershipDialog>("Domain Membership", hubPath: "hubs/domainmembership");

    private async Task Update() => await ExecuteAction<UpdateDialog>("Update", hubPath: "hubs/updater");

    private async Task FileUpload() => await ExecuteAction<FileUploadDialog>("Upload File");

    private async Task MessageBox() => await ExecuteAction<MessageBoxDialog>("Message Box");

    private async Task RenewCertificate() => await ExecuteAction<RenewCertificateDialog>("Renew Certificate", hubPath: "hubs/certificate");

    private async Task ExecuteAction<TDialog>(string title, bool onlyAvailable = true, bool startConnection = true, string hubPath = "hubs/control", DialogOptions? dialogOptions = null, bool requireConnections = true) where TDialog : ComponentBase
    {
        var computers = onlyAvailable ? _selectedComputers.Where(c => _availableComputers.ContainsKey(c.IpAddress)).ToList() : _selectedComputers.ToList();

        if (computers.Count == 0)
        {
            return;
        }

        dialogOptions ??= new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var dialogParameters = new DialogParameters
        {
            { nameof(CommonDialogWrapper<TDialog>.Hosts), new ConcurrentDictionary<ComputerDto, HubConnection?>(computers.ToDictionary(c => c, _ => (HubConnection?)null)) },
            { nameof(CommonDialogWrapper<TDialog>.HubPath), hubPath },
            { nameof(CommonDialogWrapper<TDialog>.StartConnection), startConnection },
            { nameof(CommonDialogWrapper<TDialog>.RequireConnections), requireConnections }
        };

        await ExecuteDialog<TDialog>(title, dialogParameters, dialogOptions);
    }

    private async Task Refresh()
    {
        if (_selectedNode is { } orgUnit)
        {
            foreach (var computer in orgUnit.Computers)
            {
                if (!_availableComputers.ContainsKey(computer.IpAddress) && !_unavailableComputers.ContainsKey(computer.IpAddress))
                {
                    continue;
                }

                var computerDto = new ComputerDto(computer.Name, computer.IpAddress, computer.MacAddress)
                {
                    Id = computer.Id,
                    OrganizationId = computer.Parent.OrganizationId,
                    OrganizationalUnitId = computer.ParentId,
                    Thumbnail = null
                };

                await LogonComputer(computerDto);
            }
        }
    }

    private async Task OpenTaskManager()
    {
        await OpenComputerWindow("taskmanager");
    }

    private async Task OpenFileManager()
    {
        await OpenComputerWindow("filemanager");
    }

    private async Task OpenLogsManager()
    {
        await OpenComputerWindow("logs");
    }

    private async Task OpenComputerWindow(string path, uint width = 800, uint height = 800)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");

        foreach (var url in _selectedComputers.Select(computer => $"/{computer.IpAddress}/{path}"))
        {
            await module.InvokeVoidAsync("openNewWindow", url, width, height);
        }
    }

    private async Task RemoveComputers()
    {
        var confirmation = await DialogService.ShowMessageBox("Delete Confirmation", "Are you sure you want to delete the selected computers?", "Yes", "No");

        if (confirmation.HasValue && confirmation.Value)
        {
            var computersToRemove = _selectedComputers.ToList();

            if (computersToRemove.Count != 0)
            {
                foreach (var computer in computersToRemove)
                {
                    await OrganizationRepository.RemoveComputerAsync(computer.OrganizationId, computer.OrganizationalUnitId, computer.Id);
                    await OrganizationRepository.SaveChangesAsync();

                    _availableComputers.TryRemove(computer.IpAddress, out _);
                    _unavailableComputers.TryRemove(computer.IpAddress, out _);
                    _pendingComputers.TryRemove(computer.IpAddress, out _);
                }

                _selectedComputers.Clear();

                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task OpenHostInfo()
    {
        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var dialogParameters = new DialogParameters
        {
            { "Host", _selectedComputers.First() }
        };

        await DialogService.ShowAsync<HostDialog>("Host Info", dialogParameters, dialogOptions);
    }

    private async Task RemoteExecutor()
    {
        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        await DialogService.ShowAsync<RemoteCommandDialog>("Remote Command", dialogOptions);
    }

    private async Task OpenMoveDialog()
    {
        var computers = _selectedComputers.ToDictionary(c => c, _ => (HubConnection?)null);
        var hosts = new ConcurrentDictionary<ComputerDto, HubConnection?>(computers);

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var additionalParameters = new Dictionary<string, object>
        {
            { "OnNodesMoved", EventCallback.Factory.Create<IEnumerable<ComputerDto>>(this, OnNodesMoved) }
        };

        var dialogParameters = new DialogParameters
        {
            { nameof(CommonDialogWrapper<MoveDialog>.Hosts), hosts },
            { nameof(CommonDialogWrapper<MoveDialog>.HubPath), "hubs/control" },
            { nameof(CommonDialogWrapper<MoveDialog>.StartConnection), true },
            { nameof(CommonDialogWrapper<MoveDialog>.RequireConnections), true },
            { nameof(CommonDialogWrapper<MoveDialog>.AdditionalParameters), additionalParameters }
        };

        await ExecuteDialog<MoveDialog>("Move", dialogParameters, dialogOptions);
    }

    private async Task OnNodesMoved(IEnumerable<ComputerDto> movedNodes)
    {
        foreach (var movedNode in movedNodes)
        {
            _selectedComputers.RemoveAll(c => c.Id == movedNode.Id);
            _availableComputers.TryRemove(movedNode.IpAddress, out _);
            _unavailableComputers.TryRemove(movedNode.IpAddress, out _);
            _pendingComputers.TryRemove(movedNode.IpAddress, out _);
        }

        await InvokeAsync(StateHasChanged);
    }

    private static IEnumerable<ComputerDto> GetSortedComputers(ConcurrentDictionary<string, ComputerDto> computers)
    {
        return computers.Values.OrderBy(computer => computer.Name);
    }

    private bool CanSelectAll(ConcurrentDictionary<string, ComputerDto> computers)
    {
        return computers.Any(computer => !_selectedComputers.Contains(computer.Value));
    }

    private bool CanDeselectAll(ConcurrentDictionary<string, ComputerDto> computers)
    {
        return computers.Any(computer => _selectedComputers.Contains(computer.Value));
    }

    private void ResetSelections()
    {
        foreach (var computer in _selectedComputers.ToList())
        {
            SelectComputer(computer, false);
        }
    }
}