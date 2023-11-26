// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.FluentUI.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Server.Components.Panels;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

public partial class Home
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private IDatabaseService DatabaseService { get; set; }

    [Inject]
    private ILogger<Home> Logger { get; set; } = default!;

    private readonly List<Computer> _selectedComputers;
    private HashSet<Node> _nodes;
    private FluentTreeItem? _currentSelected;

    public Home()
    {
        _nodes = [];
        _selectedComputers = [];
    }

    protected async override Task OnInitializedAsync()
    {
        _nodes = (await DatabaseService.GetNodesAsync(f => f.Parent == null && f is Folder)).Cast<Node>().ToHashSet();

        await base.OnInitializedAsync();
    }

    private async Task Power()
    {
        var hosts = await GetComputers(false);

        await DialogService.ShowDialogAsync<PowerDialog>(hosts, new DialogParameters()
        {
            Title = $"Power",
            TrapFocus = false,
            PreventDismissOnOverlayClick = true,
            PreventScroll = true
        });
    }

    private async Task Connect()
    {
        var hosts = await GetComputers();

        if (hosts.Count > 0)
        {
            await DialogService.ShowDialogAsync<ConnectDialog>(hosts, new DialogParameters
            {
                Title = $"Connect",
                TrapFocus = false,
                PreventDismissOnOverlayClick = true,
                PreventScroll = true
            });
        }
    }

    private async Task ConnectToShell()
    {
        var hosts = await GetComputers();

        if (hosts.Count > 0)
        {
            await DialogService.ShowDialogAsync<ShellDialog>(hosts, new DialogParameters
            {
                Title = $"Connect to shell",
                TrapFocus = false,
                PreventDismissOnOverlayClick = true,
                PreventScroll = true
            });
        }
    }

    private async Task ExecuteScript()
    {
        var hosts = await GetComputers();

        if (hosts.Count > 0)
        {
            await DialogService.ShowDialogAsync<ScriptDialog>(hosts, new DialogParameters
            {
                Title = $"Execute script",
                TrapFocus = false,
                PreventDismissOnOverlayClick = true,
                PreventScroll = true
            });
        }
    }

    private async Task OpenHomePanelAsync()
    {
        var parameters = new DialogParameters
        {
            Title = "RemoteMaster",
            Alignment = HorizontalAlignment.Left,
            Modal = true,
            TrapFocus = false,
            Width = "350px",
            PrimaryAction = null,
            SecondaryAction = null
        };

        await DialogService.ShowPanelAsync<HomePanel>(parameters);
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

            var accessToken = HttpContextAccessor.HttpContext?.Request.Cookies["accessToken"];

            HubConnection? connection = null;

            if (isAvailable)
            {
                try
                {
                    connection = new HubConnectionBuilder()
                        .WithUrl($"https://{computer.IPAddress}:5076/hubs/control", options =>
                        {
                            options.Headers.Add("Authorization", $"Bearer {accessToken}");
                        })
                        .AddMessagePackProtocol()
                        .Build();

                    await connection.StartAsync();
                }
                catch
                {
                    connection = null;
                }
            }

            lock (computerConnections)
            {
                computerConnections.Add(computer, connection);
            }
        });

        await Task.WhenAll(tasks);

        return computerConnections;
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

    private async Task OnNodeSelected(FluentTreeItem item)
    {
        _selectedComputers.Clear();

        _currentSelected = item;

        if (_nodes.FirstOrDefault(x => x.Name == _currentSelected?.Text) is Folder folder)
        {
            await UpdateComputersThumbnailsAsync(folder.Nodes.OfType<Computer>());
        }
    }
}
