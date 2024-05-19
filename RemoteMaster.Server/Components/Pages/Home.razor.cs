// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
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
    private Node? _selectedNode;
    private HashSet<Node>? _nodes;

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
        await InitializeUserAsync();
        _nodes = new HashSet<Node>(await LoadNodesWithChildren());
        await AccessTokenProvider.GetAccessTokenAsync();
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
            var user = await UserManager.FindByIdAsync(userId);

            if (user != null)
            {
                var roles = await UserManager.GetRolesAsync(user);

                foreach (var role in roles)
                {
                    userInfo.Roles.Add(role);
                }
            }
        }

        userInfo.UserName = userPrincipal.Identity?.Name ?? "UnknownUser";

        return userInfo;
    }

    private async Task<IEnumerable<Node>> LoadNodesWithChildren(Guid? parentId = null)
    {
        var units = await DatabaseService.GetNodesAsync(node => node.ParentId == parentId);

        foreach (var unit in units.OfType<OrganizationalUnit>())
        {
            unit.Nodes = new HashSet<Node>(await LoadNodesWithChildren(unit.NodeId));
        }

        return units;
    }

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private async Task OpenHostConfig()
    {
        var dialogOptions = new DialogOptions
        {
            CloseOnEscapeKey = true
        };

        await DialogService.ShowAsync<HostConfigurationGenerator>("Host Configuration Generator", dialogOptions);
    }

    private async Task PublishCrl()
    {
        var crl = await CrlService.GenerateCrlAsync();
        var result = await CrlService.PublishCrlAsync(crl);

        Snackbar.Add(result ? "CRL successfully published" : "Failed to publish CRL", result ? Severity.Success : Severity.Error);
    }

    private void ManageProfile() => NavigationManager.NavigateTo("/Account/Manage");

    private void Logout() => NavigationManager.NavigateTo("/Account/Logout");

    private async Task OnNodeSelected(Node? node)
    {
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
        var computers = orgUnit.Nodes.OfType<Computer>().ToList();

        var newPendingComputers = new ConcurrentDictionary<string, Computer>();

        foreach (var computer in computers)
        {
            if (!_availableComputers.ContainsKey(computer.IpAddress) && !_unavailableComputers.ContainsKey(computer.IpAddress))
            {
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

                await _retryPolicy.ExecuteAsync(async (ct) => await connection.InvokeAsync("ConnectAs", connectionRequest), cts.Token);
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
        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{computer.IpAddress}:5001/{hubPath}", options =>
            {
                options.AccessTokenProvider = async () => await AccessTokenProvider.GetAccessTokenAsync();
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
        await DialogService.ShowAsync<TDialog>(title, parameters, options);
    }

    private async Task Power() => await ExecuteAction<PowerDialog>("Power");

    private async Task WakeUp() => await ExecuteAction<WakeUpDialog>("Wake Up", false, false);

    private async Task Connect() => await ExecuteAction<ConnectDialog>("Connect");

    private async Task OpenShell() => await ExecuteAction<OpenShellDialog>("Open Shell", onlyAvailable: false);

    private async Task ExecuteScript() => await ExecuteAction<ScriptExecutorDialog>("Execute Script", true, dialogOptions: new DialogOptions
    {
        MaxWidth = MaxWidth.ExtraExtraLarge,
        FullWidth = true
    });

    private async Task ManagePsExecRules() => await ExecuteAction<PsExecRulesDialog>("PSExec Rules", true, dialogOptions: new DialogOptions
    {
        MaxWidth = MaxWidth.ExtraExtraLarge,
        FullWidth = true
    });

    private async Task SetMonitorState() => await ExecuteAction<MonitorStateDialog>("Set Monitor State");

    private async Task ScreenRecorder() => await ExecuteAction<ScreenRecorderDialog>("Screen Recorder", hubPath: "hubs/screenrecorder");

    private async Task DomainMembership() => await ExecuteAction<DomainMembershipDialog>("Domain Membership", hubPath: "hubs/domainmembership");

    private async Task Update() => await ExecuteAction<UpdateDialog>("Update", hubPath: "hubs/updater", dialogOptions: new DialogOptions
    {
        MaxWidth = MaxWidth.ExtraExtraLarge,
        FullWidth = true
    });

    private async Task FileUpload() => await ExecuteAction<FileUploadDialog>("Upload File");

    private async Task MessageBox() => await ExecuteAction<MessageBoxDialog>("Message Box");

    private async Task RenewCertificate() => await ExecuteAction<RenewCertificateDialog>("Renew Certificate");

    private async Task ExecuteAction<TDialog>(string title, bool onlyAvailable = true, bool startConnection = true, string hubPath = "hubs/control", DialogOptions? dialogOptions = null) where TDialog : ComponentBase
    {
        var computers = onlyAvailable ? _selectedComputers.Where(c => _availableComputers.ContainsKey(c.IpAddress)) : _selectedComputers;

        if (!computers.Any())
        {
            return;
        }

        var dialogParameters = new DialogParameters
        {
            { "Hosts", new ConcurrentDictionary<Computer, HubConnection?>(computers.ToDictionary(c => c, c => (HubConnection?)null)) },
            { "HubPath", hubPath },
            { "StartConnection", startConnection }
        };

        await ExecuteDialog<TDialog>(title, dialogParameters, dialogOptions);
    }

    private async Task<ConcurrentDictionary<Computer, HubConnection?>> GetComputerConnections(IEnumerable<Computer> computers, bool startConnection, string hubPath)
    {
        var computerConnections = new ConcurrentDictionary<Computer, HubConnection?>();

        var tasks = computers.Select(async computer =>
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var connection = await SetupConnection(computer, hubPath, startConnection, cts.Token);
                computerConnections.TryAdd(computer, connection);
            }
            catch (Exception ex)
            {
                Log.Error($"Error connecting to {hubPath} for {computer.IpAddress}: {ex.Message}");
                computerConnections.TryAdd(computer, null);
            }
        });

        await Task.WhenAll(tasks);

        return computerConnections;
    }

    private async Task Refresh()
    {
        if (_selectedNode is OrganizationalUnit orgUnit)
        {
            var computers = orgUnit.Nodes.OfType<Computer>().ToList();

            foreach (var computer in computers)
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

    private async Task OpenMoveDialog()
    {
        var dialogOptions = new DialogOptions
        {
            CloseOnEscapeKey = true
        };

        var dialogParameters = new DialogParameters
        {
            { "OnNodesMoved", EventCallback.Factory.Create<IEnumerable<Computer>>(this, OnNodesMoved) },
            { "Hosts", await GetComputerConnections(_selectedComputers, true, "hubs/control") }
        };

        await DialogService.ShowAsync<MoveDialog>("Move", dialogParameters, dialogOptions);
    }

    private async Task OnNodesMoved(IEnumerable<Computer> movedNodes)
    {
        _nodes = new HashSet<Node>(await LoadNodesWithChildren());

        foreach (var movedNode in movedNodes)
        {
            _selectedComputers.RemoveAll(c => c.NodeId == movedNode.NodeId);
            _availableComputers.TryRemove(movedNode.IpAddress, out _);
            _unavailableComputers.TryRemove(movedNode.IpAddress, out _);
            _pendingComputers.TryRemove(movedNode.IpAddress, out _);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OpenAddOuDialog()
    {
        var dialogOptions = new DialogOptions
        {
            CloseOnEscapeKey = true
        };

        var dialogParameters = new DialogParameters
        {
            { "OnOuAdded", EventCallback.Factory.Create<bool>(this, OnOuAdded) }
        };

        await DialogService.ShowAsync<AddOuDialog>("Add Organizational Unit", dialogParameters, dialogOptions);
    }

    private async Task OnOuAdded(bool ouAdded)
    {
        if (ouAdded)
        {
            _nodes = new HashSet<Node>(await LoadNodesWithChildren());
            await InvokeAsync(StateHasChanged);
        }
    }

    private static IEnumerable<Computer> GetSortedComputers(ConcurrentDictionary<string, Computer> computers)
    {
        return computers.Values.OrderBy(computer => computer.Name);
    }

    private bool CanSelectAll(ConcurrentDictionary<string, Computer> computers) => !computers.IsEmpty && !_selectedComputers.Intersect(computers.Values).Any();
    
    private bool CanDeselectAll(ConcurrentDictionary<string, Computer> computers) => _selectedComputers.Intersect(computers.Values).Any();
}
