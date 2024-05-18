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
        _nodes = new HashSet<Node>(await LoadOrganizationalUnitsWithChildren());
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

    private async Task<IEnumerable<Node>> LoadOrganizationalUnitsWithChildren(Guid? parentId = null)
    {
        var units = await DatabaseService.GetNodesAsync(node => node.ParentId == parentId);

        foreach (var unit in units.OfType<OrganizationalUnit>())
        {
            unit.Nodes = new HashSet<Node>(await LoadOrganizationalUnitsWithChildren(unit.NodeId));
        }

        return units;
    }

    private void DrawerToggle() => _drawerOpen = !_drawerOpen;

    private async Task OpenHostConfigurationGenerator()
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

        if (node is OrganizationalUnit organizationalUnit)
        {
            await LoadNodesAndThumbnails(organizationalUnit);
        }
    }

    private async Task LoadNodesAndThumbnails(OrganizationalUnit organizationalUnit)
    {
        await LoadNodes(organizationalUnit);
        await UpdateComputerThumbnailsAsync(organizationalUnit.Nodes.OfType<Computer>());
    }

    private async Task UpdateComputerThumbnailsAsync(IEnumerable<Computer> computers)
    {
        var tasks = computers.Select(UpdateComputerThumbnailAndAvailabilityAsync);

        await Task.WhenAll(tasks);
    }

    private async Task UpdateComputerThumbnailAndAvailabilityAsync(Computer computer)
    {
        try
        {
            var connection = await SetupHubConnection(computer, "hubs/control", true);

            connection.On<byte[]>("ReceiveThumbnail", async thumbnailBytes =>
            {
                if (thumbnailBytes.Length > 0)
                {
                    computer.Thumbnail = thumbnailBytes;
                    await MoveToAvailableIfThumbnailUpdated(computer);
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
            Log.Error("Exception in UpdateComputerThumbnailAndAvailabilityAsync for {IPAddress}: {Message}", computer.IpAddress, ex.Message);
        }
    }

    private async Task MoveToAvailableIfThumbnailUpdated(Computer computer)
    {
        if (_unavailableComputers.ContainsKey(computer.IpAddress))
        {
            _unavailableComputers.TryRemove(computer.IpAddress, out _);
            _availableComputers.TryAdd(computer.IpAddress, computer);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task UpdateComputerAvailabilityAsync(IEnumerable<Computer> computers)
    {
        var tasks = computers.Select(UpdateComputerThumbnailAndAvailabilityAsync);

        await Task.WhenAll(tasks);
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadNodes(Node node)
    {
        if (node is OrganizationalUnit organizationalUnit)
        {
            var computers = organizationalUnit.Nodes.OfType<Computer>().ToList();

            _availableComputers.Clear();
            _unavailableComputers.Clear();

            foreach (var computer in computers)
            {
                _unavailableComputers.TryAdd(computer.IpAddress, computer);
            }

            await UpdateComputerAvailabilityAsync(computers);
        }
    }

    private async Task<HubConnection> SetupHubConnection(Computer computer, string hubPath, bool startConnection)
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

    private void HandleComputerSelection(Computer computer, bool isSelected)
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

    private async Task Power() => await ExecuteActionDialog<PowerDialog>("Power");

    private async Task WakeUp() => await ExecuteActionDialog<WakeUpDialog>("Wake Up", false, false, hubPath: "hubs/wakeup");

    private async Task Connect() => await ExecuteActionDialog<ConnectDialog>("Connect");

    private async Task OpenShell() => await ExecuteActionDialog<OpenShellDialog>("Open Shell");

    private async Task ExecuteScript() => await ExecuteActionDialog<ScriptExecutorDialog>("Script Executor", extraLarge: true);

    private async Task SetMonitorState() => await ExecuteActionDialog<MonitorStateDialog>("Monitor State");

    private async Task ManagePsExecRules() => await ExecuteActionDialog<PsExecRulesDialog>("PSExec rules", extraLarge: true);

    private async Task ScreenRecorder() => await ExecuteActionDialog<ScreenRecorderDialog>("Screen Recorder");

    private async Task DomainMembership() => await ExecuteActionDialog<DomainMembershipDialog>("Domain Membership");

    private async Task Update() => await ExecuteActionDialog<UpdateDialog>("Update", extraLarge: true);

    private async Task FileUpload() => await ExecuteActionDialog<FileUploadDialog>("File Upload");

    private async Task MessageBox() => await ExecuteActionDialog<MessageBoxDialog>("MessageBox");

    private async Task Move() => await ExecuteActionDialog<MoveDialog>("Move");

    private async Task RenewCertificate() => await ExecuteActionDialog<RenewCertificateDialog>("Renew Certificate");

    private async Task ExecuteActionDialog<TDialog>(string title, bool onlyAvailable = true, bool startConnection = true, bool extraLarge = false, string hubPath = "hubs/control") where TDialog : ComponentBase
    {
        var computers = onlyAvailable ? _selectedComputers.Where(c => _availableComputers.ContainsKey(c.IpAddress)) : _selectedComputers;

        if (!computers.Any())
        {
            return;
        }

        var dialogParameters = new DialogParameters
        {
            { "Hosts", await GetComputers(computers, startConnection: startConnection, hubPath: hubPath) }
        };

        var dialogOptions = extraLarge ? new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        } : null;

        await ExecuteDialog<TDialog>(title, dialogParameters, dialogOptions);
    }

    private async Task<ConcurrentDictionary<Computer, HubConnection?>> GetComputers(IEnumerable<Computer> computers, bool startConnection = true, string hubPath = "hubs/control")
    {
        var computerConnections = new ConcurrentDictionary<Computer, HubConnection?>();

        var tasks = computers.Select(async computer =>
        {
            try
            {
                var connection = await SetupHubConnection(computer, hubPath, startConnection);
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

    private async Task HandleRefreshClick()
    {
        if (_selectedNode is OrganizationalUnit selectedOrganizationalUnit)
        {
            await LoadNodes(selectedOrganizationalUnit);
            await UpdateComputerThumbnailsAsync(selectedOrganizationalUnit.Nodes.OfType<Computer>());
        }
    }

    private async Task TaskManager()
    {
        foreach (var computer in _selectedComputers)
        {
            await OpenWindow($"/{computer.IpAddress}/taskmanager", 800, 800);
        }
    }

    private async Task FileManager()
    {
        foreach (var computer in _selectedComputers)
        {
            await OpenWindow($"/{computer.IpAddress}/filemanager", 800, 800);
        }
    }

    private async Task OpenWindow(string url, uint width, uint height)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");

        await module.InvokeVoidAsync("openNewWindow", url, width, height);
    }

    private async Task Remove()
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
