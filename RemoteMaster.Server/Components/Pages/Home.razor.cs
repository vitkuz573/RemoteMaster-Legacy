// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Retry;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

public partial class Home
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; }

    private string _username;
    private string _role;
    private bool _drawerOpen;
    private Node? _selectedNode;
    private HashSet<Node>? _nodes;

    private readonly List<Computer> _selectedComputers = [];
    private readonly ConcurrentDictionary<string, Computer> _availableComputers = new();
    private readonly ConcurrentDictionary<string, Computer> _unavailableComputers = new();

    private readonly AsyncRetryPolicy _retryPolicy;

    public Home()
    {
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(2), OnRetry);
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

        if (userPrincipal.Identity.IsAuthenticated)
        {
            var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                var user = await UserManager.FindByIdAsync(userId);
                var roles = await UserManager.GetRolesAsync(user);
                _role = roles.FirstOrDefault();
            }
        }

        _username = userPrincipal.Identity.Name;
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
        _selectedNode = node;

        if (node is OrganizationalUnit orgUnit)
        {
            await LoadComputers(orgUnit);
        }
    }

    private async Task LoadComputers(OrganizationalUnit orgUnit)
    {
        var computers = orgUnit.Nodes.OfType<Computer>().ToList();

        _availableComputers.Clear();
        _unavailableComputers.Clear();

        foreach (var computer in computers)
        {
            _unavailableComputers.TryAdd(computer.IpAddress, computer);
        }

        await UpdateComputers(computers);
    }

    private async Task UpdateComputers(IEnumerable<Computer> computers)
    {
        var tasks = computers.Select(UpdateComputer);
        await Task.WhenAll(tasks);
        await InvokeAsync(StateHasChanged);
    }

    private async Task UpdateComputer(Computer computer)
    {
        try
        {
            var connection = await SetupConnection(computer, "hubs/control", true);

            connection.On<byte[]>("ReceiveThumbnail", async thumbnailBytes =>
            {
                if (thumbnailBytes.Length > 0)
                {
                    computer.Thumbnail = thumbnailBytes;
                    await MoveToAvailable(computer);
                }
            });

            connection.On("ReceiveCloseConnection", async () =>
            {
                await connection.StopAsync();
                Log.Information("Connection closed for {IPAddress}", computer.IpAddress);
            });

            var httpContext = HttpContextAccessor.HttpContext;
            var userIdentity = httpContext?.User.Identity;

            var connectRequest = new ConnectionRequest(Intention.ReceiveThumbnail, userIdentity.Name);

            await _retryPolicy.ExecuteAsync(async () => await connection.InvokeAsync("ConnectAs", connectRequest));
        }
        catch (Exception ex)
        {
            Log.Error("Exception in UpdateComputer for {IPAddress}: {Message}", computer.IpAddress, ex.Message);
        }
    }

    private async Task MoveToAvailable(Computer computer)
    {
        if (_unavailableComputers.ContainsKey(computer.IpAddress))
        {
            _unavailableComputers.TryRemove(computer.IpAddress, out _);
            _availableComputers.TryAdd(computer.IpAddress, computer);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task<HubConnection> SetupConnection(Computer computer, string hubPath, bool startConnection)
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
            await connection.StartAsync();
            Log.Information("Connection started for {IPAddress}", computer.IpAddress);
        }

        return connection;
    }

    private void OnRetry(Exception exception, TimeSpan timeSpan, int retryCount, Context context)
    {
        Log.Warning($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
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

    private async Task WakeUp() => await ExecuteAction<WakeUpDialog>("Wake Up", false, false, "hubs/wakeup");

    private async Task Connect() => await ExecuteAction<ConnectDialog>("Connect");

    private async Task OpenShell() => await ExecuteAction<OpenShellDialog>("Open Shell");

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

    private async Task ScreenRecorder() => await ExecuteAction<ScreenRecorderDialog>("Screen Recorder");

    private async Task DomainMembership() => await ExecuteAction<DomainMembershipDialog>("Domain Membership");

    private async Task Update() => await ExecuteAction<UpdateDialog>("Update", true);

    private async Task FileUpload() => await ExecuteAction<FileUploadDialog>("Upload File");

    private async Task MessageBox() => await ExecuteAction<MessageBoxDialog>("Message Box");

    private async Task Move() => await ExecuteAction<MoveDialog>("Move");

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
            { "Hosts", await GetComputerConnections(computers, startConnection, hubPath) }
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
                var connection = await SetupConnection(computer, hubPath, startConnection);
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
            await LoadComputers(orgUnit);
        }
    }

    private async Task OpenTaskManager()
    {
        foreach (var computer in _selectedComputers)
        {
            await OpenNewWindow($"/{computer.IpAddress}/taskmanager", 800, 800);
        }
    }

    private async Task OpenFileManager()
    {
        foreach (var computer in _selectedComputers)
        {
            await OpenNewWindow($"/{computer.IpAddress}/filemanager", 800, 800);
        }
    }

    private async Task OpenNewWindow(string url, uint width, uint height)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");

        await module.InvokeVoidAsync("openNewWindow", url, width, height);
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
            }

            _selectedComputers.Clear();
        }
    }

    private static IEnumerable<Computer> GetSortedComputers(ConcurrentDictionary<string, Computer> computers)
    {
        return computers.Values.OrderBy(computer => computer.Name);
    }
}
