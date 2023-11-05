// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Components;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Pages;

public partial class Index
{
    private bool _isMenuOpen = false;

    private void ToggleDrawer()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    private HashSet<Node> _nodes;
    private Node _selectedNode;

    private readonly Dictionary<Computer, string> _scriptResults = new();

    private readonly List<Computer> _selectedComputers = new();

    [Inject]
    private IDialogService DialogService { get; set; }

    [Inject]
    private IDatabaseService DatabaseService { get; set; }

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; }

    [Inject]
    private ILogger<Index> Logger { get; set; }

    protected async override Task OnInitializedAsync()
    {
        _nodes = (await DatabaseService.GetNodesAsync(f => f.Parent == null && f is Folder)).Cast<Node>().ToHashSet();
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
        var tasks = computers.Select(UpdateComputerThumbnailAsync).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task UpdateComputerThumbnailAsync(Computer computer)
    {
        Logger.LogInformation("UpdateComputerThumbnailAsync Called for {IPAddress}", computer.IPAddress);

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

            Logger.LogInformation("Calling ConnectAs with Intention.GetThumbnail for {IPAddress}", computer.IPAddress);
            await connection.InvokeAsync("ConnectAs", Intention.GetThumbnail);
        }
        catch (Exception ex)
        {
            Logger.LogError("Exception in UpdateComputerThumbnailAsync for {IPAddress}: {Message}", computer.IPAddress, ex.Message);
        }
    }

    private async Task HandleRefreshClick()
    {
        if (_selectedNode is Folder selectedFolder)
        {
            await UpdateComputersThumbnailsAsync(selectedFolder.Nodes.OfType<Computer>());
        }
    }

    private async Task<Dictionary<Computer, HubConnection>> GetAvailableComputers()
    {
        var computerConnectionDictionary = new Dictionary<Computer, HubConnection>();

        var tasks = _selectedComputers.Select(async computer =>
        {
            if (!await IsComputerAvailable(computer))
            {
                return;
            }

            var accessToken = HttpContextAccessor.HttpContext?.Request.Cookies["accessToken"];

            var connection = new HubConnectionBuilder()
                .WithUrl($"https://{computer.IPAddress}:5076/hubs/control", options =>
                {
                    options.Headers.Add("Authorization", $"Bearer {accessToken}");
                })
                .AddMessagePackProtocol()
                .Build();

            connection.On<string>("ReceiveScriptResult", async (result) =>
            {
                _scriptResults[computer] = result;

                await InvokeAsync(StateHasChanged);
            });

            await connection.StartAsync();

            lock (computerConnectionDictionary)
            {
                computerConnectionDictionary.Add(computer, connection);
            }
        });

        await Task.WhenAll(tasks);

        return computerConnectionDictionary;
    }

    private async Task Connect()
    {
        var dialogParameters = new DialogParameters<ConnectDialog>
        {
            { x => x.Hosts, await GetAvailableComputers() }
        };

        await DialogService.ShowAsync<ConnectDialog>("Connect", dialogParameters);
    }

    private async Task OpenShell()
    {
        var dialogParameters = new DialogParameters<OpenShellDialog>
        {
            { x => x.Hosts, await GetAvailableComputers() }
        };

        await DialogService.ShowAsync<OpenShellDialog>("Connect to shell", dialogParameters);
    }

    private async Task Power()
    {
        var dialogParameters = new DialogParameters<PowerDialog>
        {
            { x => x.AvailableHosts, await GetAvailableComputers() },
            { x => x.Hosts, _selectedComputers }
        };

        await DialogService.ShowAsync<PowerDialog>("Power", dialogParameters);
    }

    private async Task DomainMember()
    {
        var dialogParameters = new DialogParameters<DomainManagementDialog>
        {
            { x => x.Hosts, await GetAvailableComputers() }
        };

        await DialogService.ShowAsync<DomainManagementDialog>("Domain Management", dialogParameters);
    }

    private async Task Update()
    {
        var dialogParameters = new DialogParameters<UpdateDialog>
        {
            { x => x.Hosts, await GetAvailableComputers() }
        };

        await DialogService.ShowAsync<UpdateDialog>("Update", dialogParameters);
    }

    private async Task ScreenRecorder()
    {
        var computers = await GetAvailableComputers();

        var dialogParameters = new DialogParameters<ScreenRecorderDialog>
        {
            { x => x.Hosts, await GetAvailableComputers() }
        };

        await DialogService.ShowAsync<ScreenRecorderDialog>("Screen Recorder", dialogParameters);
    }
    
    private async Task SetMonitorState()
    {
        var dialogParameters = new DialogParameters<MonitorStateDialog>
        {
            { x => x.Hosts, await GetAvailableComputers() }
        };

        await DialogService.ShowAsync<MonitorStateDialog>("Monitor State", dialogParameters);
    }

    private async Task ExecuteScript()
    {
        var dialogParameters = new DialogParameters<ScriptExecutorDialog>
        {
            { x => x.Hosts, await GetAvailableComputers() }
        };

        await DialogService.ShowAsync<ScriptExecutorDialog>("Script executor", dialogParameters);
    }
    
    private async Task ManagePSExecRules()
    {
        var dialogParameters = new DialogParameters<PsexecRulesDialog>
        {
            { x => x.Hosts, await GetAvailableComputers() }
        };

        await DialogService.ShowAsync<PsexecRulesDialog>("PSExec rules", dialogParameters);
    }

    private static async Task<bool> IsComputerAvailable(Computer computer)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(computer.IPAddress, 1000);

            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    private async Task OpenHostConfigGenerator()
    {
        var dialogOptions = new DialogOptions
        {
            CloseOnEscapeKey = true,
        };
        
        await DialogService.ShowAsync<HostConfigurationGenerator>("Host Configuration Generator", dialogOptions);
    }
}
