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
using Polly;
using Polly.Retry;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

public partial class Home
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; }

    private UserInfo _userInfo = new();
    private bool _drawerOpen;
    private INode? _selectedNode;
    private HashSet<INode>? _nodes;

    private readonly List<Computer> _selectedComputers = [];
    private readonly ConcurrentDictionary<string, Computer> _availableComputers = new();
    private readonly ConcurrentDictionary<string, Computer> _unavailableComputers = new();
    private readonly ConcurrentDictionary<string, Computer> _pendingComputers = new();

    private readonly AsyncRetryPolicy _retryPolicy;

    public Home()
    {
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(1), OnRetry);
    }

    protected async override Task OnInitializedAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        var userId = UserManager.GetUserId(httpContext.User);

        await InitializeUserAsync();

        _nodes = new HashSet<INode>(await LoadNodes());

        await AccessTokenProvider.GetAccessTokenAsync(userId);
    }

    private async Task InitializeUserAsync()
    {
        var authState = await AuthenticationStateTask;
        var userPrincipal = authState.User;

        if (userPrincipal?.Identity?.IsAuthenticated == true)
        {
            _userInfo = await GetUserInfoAsync(userPrincipal);
        }
    }

    private async Task<UserInfo> GetUserInfoAsync(ClaimsPrincipal userPrincipal)
    {
        var userInfo = new UserInfo();
        var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await UserManager.Users
                .Include(u => u.AccessibleOrganizations)
                .Include(u => u.AccessibleOrganizationalUnits)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                var roles = await UserManager.GetRolesAsync(user);

                foreach (var role in roles)
                {
                    userInfo.Roles.Add(role);
                }
            }
            else
            {
                Log.Warning("User with ID {UserId} not found", userId);
            }
        }
        else
        {
            Log.Warning("User ID not found in claims");
        }

        userInfo.UserName = userPrincipal.Identity?.Name ?? "UnknownUser";

        return userInfo;
    }

    private async Task<IEnumerable<INode>> LoadNodes(Guid? organizationId = null, Guid? parentId = null)
    {
        var authState = await AuthenticationStateTask;
        var userPrincipal = authState.User;
        var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Log.Warning("User ID not found in claims");

            return [];
        }

        var accessibleOrganizations = await GetAccessibleOrganizations(userId);
        var accessibleOrganizationalUnits = await GetAccessibleOrganizationalUnits(userId);
        var units = new List<INode>();

        if (organizationId == null)
        {
            var organizations = await DatabaseService.GetNodesAsync<Organization>(o => accessibleOrganizations.Contains(o.NodeId)) ?? [];
            units.AddRange(organizations);

            foreach (var organization in organizations)
            {
                organization.OrganizationalUnits = (await LoadNodes(organization.NodeId, null)).OfType<OrganizationalUnit>().ToList();
            }
        }
        else
        {
            var organizationalUnits = await DatabaseService.GetNodesAsync<OrganizationalUnit>(ou =>
                ou.OrganizationId == organizationId &&
                (parentId == null || ou.ParentId == parentId) &&
                accessibleOrganizationalUnits.Contains(ou.NodeId)) ?? [];

            var computers = await DatabaseService.GetNodesAsync<Computer>(c => c.ParentId == parentId) ?? [];

            units.AddRange(organizationalUnits);
            units.AddRange(computers);

            foreach (var unit in organizationalUnits)
            {
                unit.Children = (await LoadNodes(unit.OrganizationId, unit.NodeId)).OfType<OrganizationalUnit>().ToList();
                unit.Computers = (await LoadNodes(unit.OrganizationId, unit.NodeId)).OfType<Computer>().ToList();
            }
        }

        return units;
    }

    private async Task<List<Guid>> GetAccessibleOrganizations(string userId)
    {
        var user = await UserManager.Users
            .Include(u => u.AccessibleOrganizations)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.AccessibleOrganizations.Select(org => org.NodeId).ToList() ?? new List<Guid>();
    }

    private async Task<List<Guid>> GetAccessibleOrganizationalUnits(string userId)
    {
        var user = await UserManager.Users
            .Include(u => u.AccessibleOrganizationalUnits)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.AccessibleOrganizationalUnits.Select(ou => ou.NodeId).ToList() ?? new List<Guid>();
    }

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

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
    private async Task PublishCrl()
    {
        var crl = await CrlService.GenerateCrlAsync();
        var result = await CrlService.PublishCrlAsync(crl);

        Snackbar.Add(result ? "CRL successfully published" : "Failed to publish CRL", result ? Severity.Success : Severity.Error);
    }

    private void ManageProfile() => NavigationManager.NavigateTo("/Account/Manage");

    private void Logout() => NavigationManager.NavigateTo("/Account/Logout");

    private async Task OnNodeSelected(INode? node)
    {
        if (node is Organization)
        {
            return;
        }

        _selectedComputers.Clear();
        _availableComputers.Clear();
        _unavailableComputers.Clear();
        _pendingComputers.Clear();
        _selectedNode = node;

        if (node is OrganizationalUnit orgUnit)
        {
            await LoadComputers(orgUnit);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadComputers(OrganizationalUnit orgUnit)
    {
        var computers = orgUnit.Computers.ToList();

        var newPendingComputers = new ConcurrentDictionary<string, Computer>();

        foreach (var computer in computers)
        {
            if (!_availableComputers.ContainsKey(computer.IpAddress) && !_unavailableComputers.ContainsKey(computer.IpAddress))
            {
                computer.Thumbnail = null;
                newPendingComputers.TryAdd(computer.IpAddress, computer);
            }
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
        var channel = Channel.CreateUnbounded<Computer>();

        var logonTasks = _selectedComputers.Select(async computer =>
        {
            await LogonComputer(computer);
            await channel.Writer.WriteAsync(computer);
        });

        var readTask = Task.Run(async () =>
        {
            await foreach (var computer in channel.Reader.ReadAllAsync())
            {
                await InvokeAsync(StateHasChanged);
            }
        });

        await Task.WhenAll(logonTasks);
        channel.Writer.Complete();
        await readTask;
    }

    private async Task LogonComputer(Computer computer)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            var connection = await SetupConnection(computer, "hubs/control", true, cts.Token);

            connection.On<byte[]>("ReceiveThumbnail", async thumbnailBytes =>
            {
                if (thumbnailBytes.Length > 0)
                {
                    computer.Thumbnail = thumbnailBytes;
                    await MoveToAvailable(computer);
                }
                else
                {
                    await MoveToUnavailable(computer);
                }

                await InvokeAsync(StateHasChanged);
            });

            connection.On("ReceiveCloseConnection", async () =>
            {
                await connection.StopAsync();

                Log.Information("Connection closed for {IPAddress}", computer.IpAddress);
            });

            var userName = _userInfo.UserName;

            if (!string.IsNullOrEmpty(userName))
            {
                var connectionRequest = new ConnectionRequest(Intention.ReceiveThumbnail, userName);

                await _retryPolicy.ExecuteAsync(async (ct) => await connection.InvokeAsync("ConnectAs", connectionRequest, CancellationToken.None), cts.Token);
            }
            else
            {
                Log.Warning("User name is null or empty, unable to create connection request.");
            }
        }
        catch (OperationCanceledException)
        {
            await MoveToUnavailable(computer);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Log.Error("Exception in LogonComputer for {IPAddress}: {Message}", computer.IpAddress, ex.Message);
            await MoveToUnavailable(computer);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LogoffComputers()
    {
        var tasks = _selectedComputers
            .Where(c => _availableComputers.ContainsKey(c.IpAddress) || _unavailableComputers.ContainsKey(c.IpAddress))
            .Select(LogoffComputer);

        await Task.WhenAll(tasks);
    }

    private async Task LogoffComputer(Computer computer)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            var connection = await SetupConnection(computer, "hubs/control", false, cts.Token);

            connection.On("ReceiveCloseConnection", async () =>
            {
                await connection.StopAsync();

                Log.Information("Connection closed for {IPAddress}", computer.IpAddress);
            });

            var userName = _userInfo.UserName;

            if (!string.IsNullOrEmpty(userName))
            {
                computer.Thumbnail = null;
                await MoveToPending(computer);
            }
            else
            {
                Log.Warning("User name is null or empty, unable to create disconnection request.");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Exception in LogoffComputer for {IPAddress}: {Message}", computer.IpAddress, ex.Message);
        }
    }

    private async Task MoveToAvailable(Computer computer)
    {
        if (_pendingComputers.ContainsKey(computer.IpAddress))
        {
            _pendingComputers.TryRemove(computer.IpAddress, out _);
        }
        else if (_unavailableComputers.ContainsKey(computer.IpAddress))
        {
            _unavailableComputers.TryRemove(computer.IpAddress, out _);
        }

        _availableComputers.TryAdd(computer.IpAddress, computer);

        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveToUnavailable(Computer computer)
    {
        computer.Thumbnail = null;

        if (_pendingComputers.ContainsKey(computer.IpAddress))
        {
            _pendingComputers.TryRemove(computer.IpAddress, out _);
        }
        else if (_availableComputers.ContainsKey(computer.IpAddress))
        {
            _availableComputers.TryRemove(computer.IpAddress, out _);
        }

        _unavailableComputers.TryAdd(computer.IpAddress, computer);

        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveToPending(Computer computer)
    {
        computer.Thumbnail = null;

        if (_availableComputers.ContainsKey(computer.IpAddress))
        {
            _availableComputers.TryRemove(computer.IpAddress, out _);
        }
        else if (_unavailableComputers.ContainsKey(computer.IpAddress))
        {
            _unavailableComputers.TryRemove(computer.IpAddress, out _);
        }

        _pendingComputers.TryAdd(computer.IpAddress, computer);

        await InvokeAsync(StateHasChanged);
    }

    private async Task<HubConnection> SetupConnection(Computer computer, string hubPath, bool startConnection, CancellationToken cancellationToken)
    {
        var httpContext = HttpContextAccessor.HttpContext;
        var userId = UserManager.GetUserId(httpContext.User);

        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{computer.IpAddress}:5001/{hubPath}", options =>
            {
                options.AccessTokenProvider = async () => await AccessTokenProvider.GetAccessTokenAsync(userId);
            })
            .AddMessagePackProtocol()
            .Build();

        if (startConnection)
        {
            await connection.StartAsync(cancellationToken);
            Log.Information("Connection started for {IPAddress}", computer.IpAddress);
        }

        return connection;
    }

    private void OnRetry(Exception exception, TimeSpan timeSpan, int retryCount, Context context)
    {
        Log.Warning($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
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

    private void SelectComputer(Computer computer, bool isSelected)
    {
        if (isSelected)
        {
            if (!_selectedComputers.Contains(computer))
            {
                _selectedComputers.Add(computer);
            }
        }
        else
        {
            _selectedComputers.Remove(computer);
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
        if (_userInfo.Roles.Contains("Viewer"))
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
            await module.InvokeVoidAsync("openNewWindow", $"/{computer.IpAddress}/access?imageQuality=25&cursorTracking=true&inputEnabled=false", 600, 400);
        }
    }

    private async Task OpenShell() => await ExecuteAction<OpenShellDialog>("Open Shell", false, false, requireConnections: false);

    private async Task ExecuteScript() => await ExecuteAction<ScriptExecutorDialog>("Execute Script");

    private async Task ManagePsExecRules() => await ExecuteAction<PsExecRulesDialog>("PSExec Rules", hubPath: "hubs/service");

    private async Task SetMonitorState() => await ExecuteAction<MonitorStateDialog>("Set Monitor State");

    private async Task ScreenRecorder() => await ExecuteAction<ScreenRecorderDialog>("Screen Recorder", hubPath: "hubs/screenrecorder");

    private async Task DomainMembership() => await ExecuteAction<DomainMembershipDialog>("Domain Membership", hubPath: "hubs/domainmembership");

    private async Task Update() => await ExecuteAction<UpdateDialog>("Update", hubPath: "hubs/updater");

    private async Task FileUpload() => await ExecuteAction<FileUploadDialog>("Upload File");

    private async Task MessageBox() => await ExecuteAction<MessageBoxDialog>("Message Box");

    private async Task RenewCertificate() => await ExecuteAction<RenewCertificateDialog>("Renew Certificate");

    private async Task ExecuteAction<TDialog>(string title, bool onlyAvailable = true, bool startConnection = true, string hubPath = "hubs/control", DialogOptions? dialogOptions = null, bool requireConnections = true) where TDialog : ComponentBase
    {
        var computers = onlyAvailable ? _selectedComputers.Where(c => _availableComputers.ContainsKey(c.IpAddress)) : _selectedComputers;

        if (!computers.Any())
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
            { nameof(CommonDialogWrapper<TDialog>.Hosts), new ConcurrentDictionary<Computer, HubConnection?>(computers.ToDictionary(c => c, c => (HubConnection?)null)) },
            { nameof(CommonDialogWrapper<TDialog>.HubPath), hubPath },
            { nameof(CommonDialogWrapper<TDialog>.StartConnection), startConnection },
            { nameof(CommonDialogWrapper<TDialog>.RequireConnections), requireConnections }
        };

        await ExecuteDialog<TDialog>(title, dialogParameters, dialogOptions);
    }

    private async Task Refresh()
    {
        if (_selectedNode is OrganizationalUnit orgUnit)
        {
            foreach (var computer in orgUnit.Computers)
            {
                if (_availableComputers.ContainsKey(computer.IpAddress) || _unavailableComputers.ContainsKey(computer.IpAddress))
                {
                    await LogonComputer(computer);
                }
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

    private async Task OpenComputerWindow(string path, uint width = 800, uint height = 800)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");

        foreach (var computer in _selectedComputers)
        {
            var url = $"/{computer.IpAddress}/{path}";
            await module.InvokeVoidAsync("openNewWindow", url, width, height);
        }
    }

    private async Task RemoveComputers()
    {
        var confirmation = await DialogService.ShowMessageBox("Delete Confirmation", "Are you sure you want to delete the selected computers?", "Yes", "No");

        if (confirmation.HasValue && confirmation.Value)
        {
            foreach (var computer in _selectedComputers.ToList())
            {
                await DatabaseService.RemoveNodeAsync(computer);

                _availableComputers.TryRemove(computer.IpAddress, out _);
                _unavailableComputers.TryRemove(computer.IpAddress, out _);
                _pendingComputers.TryRemove(computer.IpAddress, out _);
            }

            _selectedComputers.Clear();
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
        var computers = _selectedComputers.ToDictionary(c => c, c => (HubConnection?)null);
        var hosts = new ConcurrentDictionary<Computer, HubConnection?>(computers);

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var additionalParameters = new Dictionary<string, object>
        {
            { "OnNodesMoved", EventCallback.Factory.Create<IEnumerable<Computer>>(this, OnNodesMoved) }
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

    private async Task OnNodesMoved(IEnumerable<Computer> movedNodes)
    {
        foreach (var movedNode in movedNodes)
        {
            _selectedComputers.RemoveAll(c => c.NodeId == movedNode.NodeId);
            _availableComputers.TryRemove(movedNode.IpAddress, out _);
            _unavailableComputers.TryRemove(movedNode.IpAddress, out _);
            _pendingComputers.TryRemove(movedNode.IpAddress, out _);
        }

        await InvokeAsync(StateHasChanged);
    }

    private static IEnumerable<Computer> GetSortedComputers(ConcurrentDictionary<string, Computer> computers)
    {
        return computers.Values.OrderBy(computer => computer.Name);
    }

    private bool CanSelectAll(ConcurrentDictionary<string, Computer> computers) => !computers.IsEmpty && _selectedComputers.Count < computers.Count;

    private bool CanDeselectAll(ConcurrentDictionary<string, Computer> computers) => _selectedComputers.Intersect(computers.Values).Any();
}
