// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

public partial class Home
{
    private string _username;
    private string _role;

    private bool _drawerOpen;
    private Node? _selectedNode;
    private HashSet<Node>? _nodes;

    private readonly List<Computer> _selectedComputers = [];
    private ConcurrentBag<Computer> _availableComputers = [];
    private ConcurrentBag<Computer> _unavailableComputers = [];

    private bool _isDarkMode = false;

    private readonly MudTheme _theme = new()
    {
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthLeft = "250px"
        }
    };

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
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

        _nodes = new HashSet<Node>(await LoadOrganizationalUnitsWithChildren());

        await AccessTokenProvider.GetAccessTokenAsync();
    }

    private async Task<IEnumerable<Node>> LoadOrganizationalUnitsWithChildren(Guid? parentId = null)
    {
        var units = await DatabaseService.GetNodesAsync(node => node.ParentId == parentId);

        foreach (var unit in units.OfType<OrganizationalUnit>())
        {
            unit.Nodes = [..await LoadOrganizationalUnitsWithChildren(unit.NodeId)];
        }

        return units;
    }

    private async Task LoadNodes(Node node)
    {
        if (node is not OrganizationalUnit organizationalUnit)
        {
            return;
        }

        var computers = organizationalUnit.Nodes.OfType<Computer>().ToList();

        _unavailableComputers = new ConcurrentBag<Computer>(computers);

        await UpdateComputerAvailabilityAsync(computers);
    }

    private async Task UpdateComputerAvailabilityAsync(IEnumerable<Computer> computers)
    {
        var tempAvailable = new ConcurrentBag<Computer>();
        var tempUnavailable = new ConcurrentBag<Computer>();

        foreach (var computer in _unavailableComputers)
        {
            tempUnavailable.Add(computer);
        }

        var tasks = computers.Select(async computer =>
        {
            var isHubAvailable = await ComputerConnectivityService.IsHubAvailable(computer, "hubs/control");

            if (isHubAvailable)
            {
                tempAvailable.Add(computer);
                tempUnavailable = new ConcurrentBag<Computer>(tempUnavailable.Where(c => c != computer));
            }
            else
            {
                if (!tempUnavailable.Contains(computer) && !_availableComputers.Contains(computer))
                {
                    tempUnavailable.Add(computer);
                }
            }
        });

        await Task.WhenAll(tasks);

        _availableComputers = new ConcurrentBag<Computer>(tempAvailable);
        _unavailableComputers = new ConcurrentBag<Computer>(tempUnavailable);

        await InvokeAsync(StateHasChanged);
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
    }

    private async Task OpenHostConfigurationGenerator()
    {
        var dialogOptions = new DialogOptions
        {
            CloseOnEscapeKey = true,
        };

        await DialogService.ShowAsync<HostConfigurationGenerator>("Host Configuration Generator", dialogOptions);
    }

    private async Task PublishCrl()
    {
        var crl = await CrlService.GenerateCrlAsync();
        var result = await CrlService.PublishCrlAsync(crl);

        if (result)
        {
            Snackbar.Add($"CRL successfully published", Severity.Success);
        }
        else
        {
            Snackbar.Add($"Failed to publish CRL", Severity.Error);
        }
    }

    private void ManageProfile()
    {
        NavigationManager.NavigateTo("/Account/Manage");
    }

    private void Logout()
    {
        NavigationManager.NavigateTo("/Account/Logout");
    }

    private async Task OnNodeSelected(Node? node)
    {
        _selectedComputers.Clear();
        _selectedNode = node;

        if (node is OrganizationalUnit organizationalUnit)
        {
            await LoadNodes(node);
            await UpdateComputersThumbnailsAsync(organizationalUnit.Nodes.OfType<Computer>());
        }
    }

    private async Task UpdateComputersThumbnailsAsync(IEnumerable<Computer> computers)
    {
        var tasks = computers.Select(UpdateComputerThumbnailAsync);
        await Task.WhenAll(tasks);
    }

    private async Task UpdateComputerThumbnailAsync(Computer computer)
    {
        Log.Information("UpdateComputerThumbnailAsync Called for {IPAddress}", computer.IpAddress);

        var isHubAvailable = await ComputerConnectivityService.IsHubAvailable(computer, "hubs/control");

        if (!isHubAvailable)
        {
            Log.Information("Hub is not available for {IPAddress}, skipping thumbnail update.", computer.IpAddress);
            return;
        }

        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{computer.IpAddress}:5001/hubs/control", options =>
            {
                options.AccessTokenProvider = async () => await AccessTokenProvider.GetAccessTokenAsync();
            })
            .AddMessagePackProtocol()
            .Build();

        try
        {
            connection.On<byte[]>("ReceiveThumbnail", async (thumbnailBytes) =>
            {
                if (thumbnailBytes.Length > 0)
                {
                    computer.Thumbnail = thumbnailBytes;
                    await InvokeAsync(StateHasChanged);
                }
            });

            connection.On("ReceiveCloseConnection", async () =>
            {
                await connection.StopAsync();
                Log.Information("Connection closed for {IPAddress}", computer.IpAddress);
            });

            await connection.StartAsync();
            Log.Information("Calling ConnectAs with Intention.ReceiveThumbnail for {IPAddress}", computer.IpAddress);
            await connection.InvokeAsync("ConnectAs", Intention.ReceiveThumbnail);
        }
        catch (Exception ex)
        {
            Log.Error("Exception in UpdateComputerThumbnailAsync for {IPAddress}: {Message}", computer.IpAddress, ex.Message);
        }
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
    }

    private async Task Power()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<PowerDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<PowerDialog>("Power", dialogParameters);
    }

    private async Task WakeUp()
    {
        var dialogParameters = new DialogParameters<WakeUpDialog>
        {
            { x => x.Hosts, await GetComputers(onlyAvailable: false, startConnection: false) }
        };

        await DialogService.ShowAsync<WakeUpDialog>("Wake Up", dialogParameters);
    }

    private async Task Connect()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<ConnectDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<ConnectDialog>("Connect", dialogParameters);
    }

    private async Task OpenWindow(string url, uint width, uint height)
    {
        await JsRuntime.InvokeVoidAsync("openNewWindow", url, width, height);
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

    private async Task OpenShell()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<OpenShellDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<OpenShellDialog>("Open Shell", dialogParameters);
    }

    private async Task ExecuteScript()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<ScriptExecutorDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        await DialogService.ShowAsync<ScriptExecutorDialog>("Script Executor", dialogParameters, dialogOptions);
    }

    private async Task HandleRefreshClick()
    {
        if (_selectedNode is OrganizationalUnit selectedOrganizationalUnit)
        {
            var computers = selectedOrganizationalUnit.Nodes.OfType<Computer>().ToList();
            var tempAvailable = new ConcurrentBag<Computer>();
            var tempUnavailable = new ConcurrentBag<Computer>();

            var tasks = computers.Select(async computer =>
            {
                var isHubAvailable = await ComputerConnectivityService.IsHubAvailable(computer, "hubs/control");

                if (isHubAvailable)
                {
                    await UpdateComputerThumbnailAsync(computer);
                    tempAvailable.Add(computer);
                }
                else
                {
                    computer.Thumbnail = null;
                    tempUnavailable.Add(computer);
                }
            });

            await Task.WhenAll(tasks);

            _availableComputers = new ConcurrentBag<Computer>(tempAvailable.OrderBy(computer => computer.Name));
            _unavailableComputers = new ConcurrentBag<Computer>(tempUnavailable.OrderBy(computer => computer.Name));

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SetMonitorState()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<MonitorStateDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<MonitorStateDialog>("Monitor State", dialogParameters);
    }

    private async Task ManagePsExecRules()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<PsExecRulesDialog>
        {
            { x => x.Hosts, await GetComputers(hubPath: "hubs/service") }
        };

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        await DialogService.ShowAsync<PsExecRulesDialog>("PSExec rules", dialogParameters, dialogOptions);
    }

    private async Task ScreenRecorder()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<ScreenRecorderDialog>
        {
            { x => x.Hosts, await GetComputers(hubPath: "hubs/screenrecorder") }
        };

        await DialogService.ShowAsync<ScreenRecorderDialog>("Screen Recorder", dialogParameters);
    }

    private async Task DomainMembership()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<DomainMembershipDialog>
        {
            { x => x.Hosts, await GetComputers(hubPath: "hubs/domainmembership") }
        };

        await DialogService.ShowAsync<DomainMembershipDialog>("Domain Membership", dialogParameters);
    }

    private async Task Update()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<UpdateDialog>
        {
            { x => x.Hosts, await GetComputers(hubPath: "hubs/updater") }
        };

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        await DialogService.ShowAsync<UpdateDialog>("Update", dialogParameters, dialogOptions);
    }

    private async Task FileUpload()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<FileUploadDialog>
        {
            { x => x.Hosts, await GetComputers(hubPath: "hubs/filemanager") }
        };

        await DialogService.ShowAsync<FileUploadDialog>("File Upload", dialogParameters);
    }

    private async Task MessageBox()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<MessageBoxDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<MessageBoxDialog>("MessageBox", dialogParameters);
    }

    private async Task Move()
    {
        var dialogParameters = new DialogParameters<MoveDialog>
        {
            { x => x.Hosts, await GetComputers(false) }
        };

        await DialogService.ShowAsync<MoveDialog>("Move", dialogParameters);
    }

    private async Task Remove()
    {
        var confirmation = await DialogService.ShowMessageBox("Delete Confirmation", "Are you sure you want to delete the selected computers?", "Yes", "No");

        if (confirmation.HasValue && confirmation.Value)
        {
            var computersToRemove = _selectedComputers.ToList();

            foreach (var computer in computersToRemove)
            {
                await DatabaseService.RemoveNodeAsync(computer);

                _availableComputers.TryTake(out _);
                _unavailableComputers.TryTake(out _);
            }

            _selectedComputers.Clear();
        }
    }

    private async Task RenewCertificate()
    {
        var dialogParameters = new DialogParameters<RenewCertificateDialog>
        {
            { x => x.Hosts, await GetComputers(false) }
        };

        await DialogService.ShowAsync<RenewCertificateDialog>("Renew Certificate", dialogParameters);
    }

    private async Task<ConcurrentDictionary<Computer, HubConnection?>> GetComputers(bool onlyAvailable = true, string hubPath = "hubs/control", bool startConnection = true)
    {
        var computerConnections = new ConcurrentDictionary<Computer, HubConnection?>();

        var tasks = _selectedComputers.Select(async computer =>
        {
            var isHubAvailable = await ComputerConnectivityService.IsHubAvailable(computer, hubPath);

            if (isHubAvailable || !onlyAvailable)
            {
                try
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

                    computerConnections.TryAdd(computer, connection);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error connecting to {hubPath} for {computer.IpAddress}: {ex.Message}");

                    if (!onlyAvailable)
                    {
                        computerConnections.TryAdd(computer, null);
                    }
                }
            }
        });

        await Task.WhenAll(tasks);

        return computerConnections;
    }
}
