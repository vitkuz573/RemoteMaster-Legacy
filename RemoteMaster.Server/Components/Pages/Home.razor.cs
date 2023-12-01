// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

#pragma warning disable CA1822

public partial class Home
{
    [Inject]
    private IBrandingService BrandingService { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private IDatabaseService DatabaseService { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    private bool _drawerOpen = false;
    private Node? _selectedNode = null;
    private HashSet<Node>? _nodes;
    private readonly List<Computer> _selectedComputers = [];

    protected async override Task OnInitializedAsync()
    {
        _nodes = (await DatabaseService.GetNodesAsync(f => f.Parent == null && f is Folder)).Cast<Node>().ToHashSet();
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task OpenHostConfigurator()
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

        if (node is Folder)
        {
            _selectedNode = node;
            await UpdateComputersThumbnailsAsync(node.Nodes.OfType<Computer>());
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
            .WithUrl($"https://{computer.IPAddress}:5076/hubs/control", options =>
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
            await connection.InvokeAsync("ConnectAs", Intention.GetThumbnail);
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
            _selectedComputers.Add(computer);
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
        var dialogParameters = new DialogParameters<ConnectDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<ConnectDialog>("Connect", dialogParameters);
    }

    private async Task OpenShell()
    {
        var dialogParameters = new DialogParameters<OpenShellDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<OpenShellDialog>("Connect to shell", dialogParameters);
    }

    private async Task ExecuteScript()
    {
        var dialogParameters = new DialogParameters<ScriptExecutorDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<ScriptExecutorDialog>("Script executor", dialogParameters);
    }

    private async Task HandleRefreshClick()
    {
        if (_selectedNode is Folder selectedFolder)
        {
            await UpdateComputersThumbnailsAsync(selectedFolder.Nodes.OfType<Computer>());
        }
    }

    private async Task SetMonitorState()
    {
        var dialogParameters = new DialogParameters<MonitorStateDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<MonitorStateDialog>("Monitor state", dialogParameters);
    }

    private async Task ManagePSExecRules()
    {
        var dialogParameters = new DialogParameters<PsexecRulesDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<PsexecRulesDialog>("PSExec rules", dialogParameters);
    }

    private async Task ScreenRecorder()
    {
        var dialogParameters = new DialogParameters<ScreenRecorderDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<ScreenRecorderDialog>("Screen Recorder", dialogParameters);
    }

    private async Task DomainMember()
    {
        var dialogParameters = new DialogParameters<DomainManagementDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<DomainManagementDialog>("Domain Management", dialogParameters);
    }

    private async Task Update()
    {
        var dialogParameters = new DialogParameters<UpdateDialog>
        {
            { x => x.Hosts, await GetComputers() }
        };

        await DialogService.ShowAsync<UpdateDialog>("Update", dialogParameters);
    }

    private async Task<Dictionary<Computer, HubConnection?>> GetComputers(bool onlyAvailable = true)
    {
        var computerConnections = new Dictionary<Computer, HubConnection?>();

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
                    .WithUrl($"https://{computer.IPAddress}:5076/hubs/control", options =>
                    {
                        options.Headers.Add("Authorization", $"Bearer {accessToken}");
                    })
                    .AddMessagePackProtocol()
                    .Build();

                await connection.StartAsync();
            }

            lock (computerConnections)
            {
                computerConnections.Add(computer, connection);
            }
        });

        await Task.WhenAll(tasks);

        return computerConnections;
    }
}
