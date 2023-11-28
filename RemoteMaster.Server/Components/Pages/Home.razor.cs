// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

#pragma warning disable CA1822

public partial class Home
{
    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; }

    private bool _drawerOpen = false;
    private Node _selectedNode;
    private readonly HashSet<Node> _nodes = [];
    private readonly List<Computer> _selectedComputers = [];

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void OpenHostConfigurator()
    {

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
}
