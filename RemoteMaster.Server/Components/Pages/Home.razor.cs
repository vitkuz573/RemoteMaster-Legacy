// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

#pragma warning disable CA1822

public partial class Home
{
    private bool _drawerOpen = false;
    private Node? _selectedNode = null;
    private HashSet<Node>? _nodes;
    private List<Computer> _selectedComputers = [];
    private List<Computer> _availableComputers = [];
    private List<Computer> _unavailableComputers = [];

    private bool _isDarkMode = true;

    private readonly MudTheme _theme = new()
    {
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthLeft = "250px"
        }
    };

    protected async override Task OnInitializedAsync()
    {
        _nodes = (await DatabaseService.GetNodesAsync(f => f.Parent == null && f is Group)).Cast<Node>().ToHashSet();
    }

    private void LoadAndGroupComputers(Node node)
    {
        if (node is Group group)
        {
            var computers = group.Nodes.OfType<Computer>().ToList();
            _availableComputers = computers; // Initially, assume all computers are available
            _unavailableComputers.Clear();

            // Update availability in the background
            _ = UpdateComputerAvailabilityAsync(computers);
        }
    }

    private async Task UpdateComputerAvailabilityAsync(List<Computer> computers)
    {
        foreach (var computer in computers)
        {
            var isAvailable = await computer.IsAvailable();

            if (!isAvailable)
            {
                _availableComputers.Remove(computer);
                _unavailableComputers.Add(computer);
            }

            await InvokeAsync(StateHasChanged); // Update UI for each computer
        }
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task OpenHostConfigurationGenerator()
    {
        var dialogOptions = new DialogOptions
        {
            CloseOnEscapeKey = true,
        };

        await DialogService.ShowAsync<HostConfigurationGenerator>("Host Configuration Generator", dialogOptions);
    }

    private void Logout()
    {

    }

    private async Task OnNodeSelected(Node node)
    {
        _selectedComputers.Clear();
        _selectedNode = node;

        if (node is Group group)
        {
            LoadAndGroupComputers(node);
            await UpdateComputersThumbnailsAsync(group.Nodes.OfType<Computer>());
        }

        StateHasChanged();
    }

    private async Task UpdateComputersThumbnailsAsync(IEnumerable<Computer> computers)
    {
        var tasks = computers.Select(UpdateComputerThumbnailAsync);
        await Task.WhenAll(tasks);
    }

    private async Task UpdateComputerThumbnailAsync(Computer computer)
    {
        Log.Information("UpdateComputerThumbnailAsync Called for {IPAddress}", computer.IPAddress);

        var accessToken = HttpContextAccessor.HttpContext?.Request.Cookies["accessToken"];

        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{computer.IPAddress}:5001/hubs/control", options =>
            {
                options.Headers.Add("Authorization", $"Bearer {accessToken}");
            })
            .AddMessagePackProtocol()
            .Build();

        try
        {
            connection.On<byte[]>("ReceiveThumbnail", async (thumbnailBytes) =>
            {
                if (thumbnailBytes?.Length > 0)
                {
                    computer.Thumbnail = thumbnailBytes;
                    await InvokeAsync(StateHasChanged);
                }
            });

            await connection.StartAsync();

            Log.Information("Calling ConnectAs with Intention.GetThumbnail for {IPAddress}", computer.IPAddress);
            await connection.InvokeAsync("ConnectAs", Intention.ReceiveThumbnail);
        }
        catch (Exception ex)
        {
            Log.Error("Exception in UpdateComputerThumbnailAsync for {IPAddress}: {Message}", computer.IPAddress, ex.Message);
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

        StateHasChanged();
    }

    private async Task Power()
    {
        var dialogParameters = new DialogParameters<PowerDialog>
        {
            { x => x.Hosts, await GetComputers(false) }
        };

        await DialogService.ShowAsync<PowerDialog>("Power", dialogParameters);
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

    protected async Task OpenWindow(string url)
    {
        await JSRuntime.InvokeVoidAsync("openNewWindow", url);
    }

    private async Task TaskManager()
    {
        foreach (var computer in _selectedComputers)
        {
            await OpenWindow($"/{computer.IPAddress}/taskmanager");
        }
    }

    private async Task FileManager()
    {
        foreach (var computer in _selectedComputers)
        {
            await OpenWindow($"/{computer.IPAddress}/filemanager");
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

        await DialogService.ShowAsync<OpenShellDialog>("Connect to shell", dialogParameters);
    }

    private async Task ExecuteScript()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true
        };

        var dialogParameters = new DialogParameters<ScriptExecutorDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<ScriptExecutorDialog>("Script Executor", dialogParameters, dialogOptions);
    }

    private async Task HandleRefreshClick()
    {
        if (_selectedNode is Group selectedFolder)
        {
            await UpdateComputersThumbnailsAsync(selectedFolder.Nodes.OfType<Computer>());
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

        await DialogService.ShowAsync<MonitorStateDialog>("Monitor state", dialogParameters);
    }

    private async Task ManagePSExecRules()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<PsexecRulesDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<PsexecRulesDialog>("PSExec rules", dialogParameters);
    }

    private async Task ScreenRecorder()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<ScreenRecorderDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<ScreenRecorderDialog>("Screen Recorder", dialogParameters);
    }

    private async Task DomainMember()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<DomainManagementDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<DomainManagementDialog>("Domain Management", dialogParameters);
    }

    private async Task Update()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<UpdateDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<UpdateDialog>("Update", dialogParameters);
    }

    private async Task FileUpload()
    {
        if (_selectedComputers.All(computer => !_availableComputers.Contains(computer)))
        {
            return;
        }

        var dialogParameters = new DialogParameters<FileUploadDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<FileUploadDialog>("File Upload", dialogParameters);
    }

    private async Task Move()
    {
        var dialogParameters = new DialogParameters<MoveDialog>
        {
            { x => x.Hosts, await GetComputers(false) }
        };

        await DialogService.ShowAsync<MoveDialog>("Move", dialogParameters);
    }

    private async Task<ConcurrentDictionary<Computer, HubConnection?>> GetComputers(bool onlyAvailable = true)
    {
        var computerConnections = new ConcurrentDictionary<Computer, HubConnection?>();

        var tasks = _selectedComputers.Select(async computer =>
        {
            var isAvailable = await computer.IsAvailable();

            if (!isAvailable && onlyAvailable)
            {
                return;
            }

            HubConnection? connection = null;

            if (isAvailable)
            {
                var accessToken = HttpContextAccessor.HttpContext?.Request.Cookies["accessToken"];

                connection = new HubConnectionBuilder()
                    .WithUrl($"https://{computer.IPAddress}:5001/hubs/control", options =>
                    {
                        options.Headers.Add("Authorization", $"Bearer {accessToken}");
                    })
                    .AddMessagePackProtocol()
                    .Build();

                await connection.StartAsync();
            }

            computerConnections.AddOrUpdate(computer, connection, (key, existingConn) => connection);
        });

        await Task.WhenAll(tasks);

        return computerConnections;
    }

    private async Task CheckSelectedComputersStatus()
    {
        var tempAvailable = _availableComputers.ToList();
        var tempUnavailable = _unavailableComputers.ToList();

        foreach (var computer in _selectedComputers.ToList())
        {
            var isAvailable = await computer.IsAvailable();

            if (isAvailable)
            {
                if (tempUnavailable.Remove(computer))
                {
                    tempAvailable.Add(computer);
                }
            }
            else
            {
                if (tempAvailable.Remove(computer))
                {
                    tempUnavailable.Add(computer);
                }
            }
        }

        _availableComputers = tempAvailable;
        _unavailableComputers = tempUnavailable;

        _selectedComputers = _selectedComputers.Where(computer => _availableComputers.Contains(computer) || _unavailableComputers.Contains(computer)).ToList();

        StateHasChanged();
    }

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
    }
}
