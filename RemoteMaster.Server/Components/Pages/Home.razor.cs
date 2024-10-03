// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Claims;
using System.Threading.Channels;
using MessagePack;
using MessagePack.Resolvers;
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
using RemoteMaster.Shared.Formatters;
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

    private readonly List<HostDto> _selectedHosts = [];
    private readonly ConcurrentDictionary<IPAddress, HostDto> _availableHosts = new();
    private readonly ConcurrentDictionary<IPAddress, HostDto> _unavailableHosts = new();
    private readonly ConcurrentDictionary<IPAddress, HostDto> _pendingHosts = new();

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

    private async Task<IEnumerable<Organization>> LoadNodes()
    {
        if (_currentUser == null)
        {
            return [];
        }

        return await OrganizationService.GetOrganizationsWithAccessibleUnitsAsync(_currentUser.Id);
    }

    private void ToggleDrawer() => DrawerOpen = !DrawerOpen;

    [Authorize(Roles = "Administrator")]
    private void OpenCertificateRenewTasks() => NavigationManager.NavigateTo("/certificates/tasks");


    [Authorize(Roles = "Administrator")]
    private async Task PublishCrl()
    {
        var crlResult = await CrlService.GenerateCrlAsync();

        if (!crlResult.IsSuccess)
        {
            Log.Error("Failed to generate CRL: {Message}", crlResult.Errors.FirstOrDefault()?.Message);
            SnackBar.Add("Failed to generate CRL", Severity.Error);
            
            return;
        }

        var publishResult = await CrlService.PublishCrlAsync(crlResult.Value);

        if (publishResult.IsSuccess)
        {
            SnackBar.Add("CRL successfully published", Severity.Success);
        }
        else
        {
            Log.Error("Failed to publish CRL: {Message}", publishResult.Errors.FirstOrDefault()?.Message);
            SnackBar.Add("Failed to publish CRL", Severity.Error);
        }
    }

    private void ManageProfile() => NavigationManager.NavigateTo("/Account/Manage");

    private void Logout() => NavigationManager.NavigateTo("/Account/Logout");

    private async Task OnNodeSelected(object? node)
    {
        _selectedHosts.Clear();
        _availableHosts.Clear();
        _unavailableHosts.Clear();
        _pendingHosts.Clear();

        switch (node)
        {
            case Organization:
                break;
            case OrganizationalUnit orgUnit:
                _selectedNode = orgUnit;
                await LoadHosts(orgUnit);
                break;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadHosts(OrganizationalUnit orgUnit)
    {
        var hosts = orgUnit.Hosts.ToList();

        var newPendingHosts = new ConcurrentDictionary<IPAddress, HostDto>();

        foreach (var host in hosts.Where(host => !_availableHosts.ContainsKey(host.IpAddress) && !_unavailableHosts.ContainsKey(host.IpAddress)))
        {
            var hostDto = new HostDto(host.Name, host.IpAddress, host.MacAddress)
            {
                Id = host.Id,
                OrganizationId = host.Parent.OrganizationId,
                OrganizationalUnitId = host.ParentId,
                Thumbnail = null
            };

            newPendingHosts.TryAdd(hostDto.IpAddress, hostDto);
        }

        _pendingHosts.Clear();

        foreach (var kvp in newPendingHosts)
        {
            _pendingHosts.TryAdd(kvp.Key, kvp.Value);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task LogonHosts()
    {
        var channel = Channel.CreateUnbounded<HostDto>();

        var logonTasks = _selectedHosts.Select(async host =>
        {
            await LogonHost(host);
            await channel.Writer.WriteAsync(host);
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

    private async Task LogonHost(HostDto hostDto)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var cancellationToken = cts.Token;

        try
        {
            const string url = "hubs/control?thumbnail=true";

            var connection = await SetupConnection(hostDto, url, true, cancellationToken);

            connection.On<byte[]>("ReceiveThumbnail", async thumbnailBytes =>
            {
                if (thumbnailBytes.Length > 0)
                {
                    hostDto.Thumbnail = thumbnailBytes;

                    await MoveToAvailable(hostDto);
                }
                else
                {
                    await MoveToUnavailable(hostDto);
                }

                await InvokeAsync(StateHasChanged);
            });

            connection.On("ReceiveCloseConnection", async () =>
            {
                await connection.StopAsync(cancellationToken);
            });
        }
        catch (OperationCanceledException)
        {
            await MoveToUnavailable(hostDto);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Log.Error("Exception in LogonHost for {IPAddress}: {Message}", hostDto.IpAddress, ex.Message);

            await MoveToUnavailable(hostDto);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LogoffHosts()
    {
        var tasks = _selectedHosts
            .Where(c => _availableHosts.ContainsKey(c.IpAddress) || _unavailableHosts.ContainsKey(c.IpAddress))
            .Select(LogoffHost);

        await Task.WhenAll(tasks);

        ResetSelections();
    }

    private async Task LogoffHost(HostDto hostDto)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var cancellationToken = cts.Token;

        try
        {
            var connection = await SetupConnection(hostDto, "hubs/control", false, cancellationToken);

            connection.On("ReceiveCloseConnection", async () =>
            {
                await connection.StopAsync(cancellationToken);
            });

            hostDto.Thumbnail = null;

            await MoveToPending(hostDto);
        }
        catch (Exception ex)
        {
            Log.Error("Exception in LogoffHost for {IPAddress}: {Message}", hostDto.IpAddress, ex.Message);
        }
    }

    private async Task MoveToAvailable(HostDto hostDto)
    {
        if (_pendingHosts.ContainsKey(hostDto.IpAddress))
        {
            _pendingHosts.TryRemove(hostDto.IpAddress, out _);
        }
        else if (_unavailableHosts.ContainsKey(hostDto.IpAddress))
        {
            _unavailableHosts.TryRemove(hostDto.IpAddress, out _);
        }

        _availableHosts.TryAdd(hostDto.IpAddress, hostDto);

        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveToUnavailable(HostDto hostDto)
    {
        hostDto.Thumbnail = null;

        if (_pendingHosts.ContainsKey(hostDto.IpAddress))
        {
            _pendingHosts.TryRemove(hostDto.IpAddress, out _);
        }
        else if (_availableHosts.ContainsKey(hostDto.IpAddress))
        {
            _availableHosts.TryRemove(hostDto.IpAddress, out _);
        }

        _unavailableHosts.TryAdd(hostDto.IpAddress, hostDto);

        await InvokeAsync(StateHasChanged);
    }

    private async Task MoveToPending(HostDto hostDto)
    {
        hostDto.Thumbnail = null;

        if (_availableHosts.ContainsKey(hostDto.IpAddress))
        {
            _availableHosts.TryRemove(hostDto.IpAddress, out _);
        }
        else if (_unavailableHosts.ContainsKey(hostDto.IpAddress))
        {
            _unavailableHosts.TryRemove(hostDto.IpAddress, out _);
        }

        _pendingHosts.TryAdd(hostDto.IpAddress, hostDto);

        await InvokeAsync(StateHasChanged);
    }

    private async Task<bool> ShowSslWarningDialog(SslPolicyErrors sslPolicyErrors)
    {
        var parameters = new DialogParameters<SslWarningDialog>
        {
            { d => d.SslPolicyErrors, sslPolicyErrors }
        };

        var dialog = await DialogService.ShowAsync<SslWarningDialog>("SSL Certificate Warning", parameters);
        var result = await dialog.Result;

        return !result.Canceled;
    }

    private async Task<HubConnection> SetupConnection(HostDto hostDto, string hubPath, bool startConnection, CancellationToken cancellationToken)
    {
        var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{hostDto.IpAddress}:5001/{hubPath}", options =>
            {
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = (_, _, _, sslPolicyErrors) =>
                        {
                            return sslPolicyErrors == SslPolicyErrors.None || Task.Run(() => ShowSslWarningDialog(sslPolicyErrors), cancellationToken).Result;
                        };
                    }

                    return handler;
                };

                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                    return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                };
            })
            .AddMessagePackProtocol(options =>
            {
                var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            })
            .Build();

        if (!startConnection)
        {
            return connection;
        }

        await connection.StartAsync(cancellationToken);

        return connection;
    }

    private void SelectAllPendingHosts()
    {
        foreach (var host in _pendingHosts.Values)
        {
            SelectHost(host, true);
        }
    }

    private void DeselectAllPendingHosts()
    {
        foreach (var host in _pendingHosts.Values)
        {
            SelectHost(host, false);
        }
    }

    private void SelectAllAvailableHosts()
    {
        foreach (var host in _availableHosts.Values)
        {
            SelectHost(host, true);
        }
    }

    private void DeselectAllAvailableHosts()
    {
        foreach (var host in _availableHosts.Values)
        {
            SelectHost(host, false);
        }
    }

    private void SelectAllUnavailableHosts()
    {
        foreach (var host in _unavailableHosts.Values)
        {
            SelectHost(host, true);
        }
    }

    private void DeselectAllUnavailableHosts()
    {
        foreach (var host in _unavailableHosts.Values)
        {
            SelectHost(host, false);
        }
    }

    private void SelectHost(HostDto hostDto, bool isSelected)
    {
        if (isSelected)
        {
            if (!_selectedHosts.Contains(hostDto))
            {
                _selectedHosts.Add(hostDto);
            }
        }
        else
        {
            _selectedHosts.Remove(hostDto);
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
        var hosts = _selectedHosts.Where(c => _availableHosts.ContainsKey(c.IpAddress)).ToList();

        foreach (var host in hosts)
        {
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");
            await module.InvokeVoidAsync("openNewWindow", $"/{host.IpAddress}/access?frameRate=60&imageQuality=25&cursorTracking=true&inputEnabled=false", 600, 400);
        }
    }

    private async Task Lock() => await ExecuteAction<LockWorkStationDialog>("Lock Workstation");

    private async Task OpenShell() => await ExecuteAction<OpenShellDialog>("Open Shell", false, false, requireConnections: false);

    private async Task ExecuteScript() => await ExecuteAction<ScriptExecutorDialog>("Script Executor");

    private async Task ManagePsExecRules() => await ExecuteAction<PsExecRulesDialog>("PSExec Rules", hubPath: "hubs/service");

    private async Task AppLauncher() => await ExecuteAction<AppLauncherDialog>("App Launcher");

    private async Task SetMonitorState() => await ExecuteAction<MonitorStateDialog>("Set Monitor State");

    private async Task ScreenRecorder() => await ExecuteAction<ScreenRecorderDialog>("Screen Recorder", hubPath: "hubs/screenrecorder");

    private async Task DomainMembership() => await ExecuteAction<DomainMembershipDialog>("Domain Membership", hubPath: "hubs/domainmembership");

    private async Task Update() => await ExecuteAction<UpdateDialog>("Update", hubPath: "hubs/updater");

    private async Task FileUpload() => await ExecuteAction<FileUploadDialog>("Upload File");

    private async Task MessageBox() => await ExecuteAction<MessageBoxDialog>("Message Box");

    private async Task RenewCertificate() => await ExecuteAction<RenewCertificateDialog>("Renew Certificate", hubPath: "hubs/certificate");

    private async Task ExecuteAction<TDialog>(string title, bool onlyAvailable = true, bool startConnection = true, string hubPath = "hubs/control", DialogOptions? dialogOptions = null, bool requireConnections = true) where TDialog : ComponentBase
    {
        var hosts = onlyAvailable ? _selectedHosts.Where(c => _availableHosts.ContainsKey(c.IpAddress)).ToList() : _selectedHosts.ToList();

        if (hosts.Count == 0)
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
            { nameof(CommonDialogWrapper<TDialog>.Hosts), new ConcurrentDictionary<HostDto, HubConnection?>(hosts.ToDictionary(c => c, _ => (HubConnection?)null)) },
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
            foreach (var host in orgUnit.Hosts)
            {
                if (!_availableHosts.ContainsKey(host.IpAddress) && !_unavailableHosts.ContainsKey(host.IpAddress))
                {
                    continue;
                }

                var hostDto = new HostDto(host.Name, host.IpAddress, host.MacAddress)
                {
                    Id = host.Id,
                    OrganizationId = host.Parent.OrganizationId,
                    OrganizationalUnitId = host.ParentId,
                    Thumbnail = null
                };

                await LogonHost(hostDto);
            }
        }
    }

    private async Task OpenTaskManager() => await OpenHostWindow("taskmanager");

    private async Task OpenDeviceManager() => await OpenHostWindow("devicemanager");

    private async Task OpenFileManager() => await OpenHostWindow("filemanager", 1120);

    private async Task OpenLogsManager() => await OpenHostWindow("logs", 1120);

    private async Task OpenChat() => await OpenHostWindow("chat");

    private async Task OpenHostWindow(string path, uint width = 800, uint height = 800)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");

        foreach (var url in _selectedHosts.Select(host => $"/{host.IpAddress}/{path}"))
        {
            await module.InvokeVoidAsync("openNewWindow", url, width, height);
        }
    }

    private async Task OpenHostRemoveDialog()
    {
        var hosts = new ConcurrentDictionary<HostDto, HubConnection?>(_selectedHosts.ToDictionary(c => c, _ => (HubConnection?)null));

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var additionalParameters = new Dictionary<string, object>
        {
            { nameof(RemoveHostsDialog.OnHostsRemoved), EventCallback.Factory.Create<IEnumerable<HostDto>>(this, OnHostsRemoved) }
        };

        var dialogParameters = new DialogParameters
        {
            { nameof(CommonDialogWrapper<RemoveHostsDialog>.Hosts), hosts },
            { nameof(CommonDialogWrapper<RemoveHostsDialog>.StartConnection), false },
            { nameof(CommonDialogWrapper<RemoveHostsDialog>.RequireConnections), false },
            { nameof(CommonDialogWrapper<RemoveHostsDialog>.AdditionalParameters), additionalParameters }
        };

        await ExecuteDialog<RemoveHostsDialog>("Remove Hosts", dialogParameters, dialogOptions);

        return;

        async Task OnHostsRemoved(IEnumerable<HostDto> removedHosts)
        {
            foreach (var removedHost in removedHosts)
            {
                _selectedHosts.RemoveAll(c => c.Id == removedHost.Id);
                _availableHosts.TryRemove(removedHost.IpAddress, out _);
                _unavailableHosts.TryRemove(removedHost.IpAddress, out _);
                _pendingHosts.TryRemove(removedHost.IpAddress, out _);
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OpenHostInfo()
    {
        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var dialogParameters = new DialogParameters<HostDialog>
        {
            { d => d.HostDto, _selectedHosts.First() }
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

    private async Task OpenHostMoveDialog()
    {
        var hosts = new ConcurrentDictionary<HostDto, HubConnection?>(_selectedHosts.ToDictionary(c => c, _ => (HubConnection?)null));

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var additionalParameters = new Dictionary<string, object>
        {
            { nameof(MoveHostsDialog.OnHostsMoved), EventCallback.Factory.Create<IEnumerable<HostDto>>(this, OnHostsMoved) }
        };

        var dialogParameters = new DialogParameters
        {
            { nameof(CommonDialogWrapper<MoveHostsDialog>.Hosts), hosts },
            { nameof(CommonDialogWrapper<MoveHostsDialog>.HubPath), "hubs/control" },
            { nameof(CommonDialogWrapper<MoveHostsDialog>.StartConnection), true },
            { nameof(CommonDialogWrapper<MoveHostsDialog>.RequireConnections), true },
            { nameof(CommonDialogWrapper<MoveHostsDialog>.AdditionalParameters), additionalParameters }
        };

        await ExecuteDialog<MoveHostsDialog>("Move Hosts", dialogParameters, dialogOptions);

        return;

        async Task OnHostsMoved(IEnumerable<HostDto> movedHosts)
        {
            foreach (var movedHost in movedHosts)
            {
                _selectedHosts.RemoveAll(c => c.Id == movedHost.Id);
                _availableHosts.TryRemove(movedHost.IpAddress, out _);
                _unavailableHosts.TryRemove(movedHost.IpAddress, out _);
                _pendingHosts.TryRemove(movedHost.IpAddress, out _);
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private static IEnumerable<HostDto> GetSortedHosts(ConcurrentDictionary<IPAddress, HostDto> hosts)
    {
        return hosts.Values.OrderBy(host => host.Name);
    }

    private bool CanSelectAll(ConcurrentDictionary<IPAddress, HostDto> hosts)
    {
        return hosts.Any(host => !_selectedHosts.Contains(host.Value));
    }

    private bool CanDeselectAll(ConcurrentDictionary<IPAddress, HostDto> hosts)
    {
        return hosts.Any(host => _selectedHosts.Contains(host.Value));
    }

    private void ResetSelections()
    {
        foreach (var host in _selectedHosts.ToList())
        {
            SelectHost(host, false);
        }
    }
}