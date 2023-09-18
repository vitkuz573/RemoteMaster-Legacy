// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using TypedSignalR.Client;

namespace RemoteMaster.Server.Pages;

public partial class Index
{
    private readonly List<Node> _entries = new();
    private Node _selectedNode;

    private bool _anyComputerSelected = false;
    private readonly List<Computer> _selectedComputers = new();

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private IConnectionManager ConnectionManager { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    protected async override Task OnInitializedAsync()
    {
        var rootFolders = DatabaseService.GetFolders().Where(f => f.Parent == null).ToList();

        foreach (var folder in rootFolders)
        {
            await LoadChildrenAsync(folder);
            _entries.Add(folder);
        }

        DatabaseService.NodeAdded += OnNodeAdded;
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

        _anyComputerSelected = _selectedComputers.Any();
        StateHasChanged();
    }

    private async Task LoadChildrenAsync(Folder folder)
    {
        var children = DatabaseService.GetFolders().Where(f => f.Parent == folder);

        foreach (var child in children)
        {
            folder.Children.Add(child);
            await LoadChildrenAsync(child);
        }
    }

    private void OnNodeAdded(object? sender, Node node)
    {
        if (node is Folder folder)
        {
            if (folder.Parent == null)
            {
                _entries.Add(folder);
            }
            else
            {
                folder.Parent.Children.Add(folder);
            }
        }

        StateHasChanged();
    }

    private void LoadComputers(TreeExpandEventArgs args)
    {
        var node = args.Value as Node;

        args.Children.Data = GetChildrenForNode(node);
        args.Children.Text = GetTextForNode;
        args.Children.HasChildren = n => n is Folder && DatabaseService.GetFolders().Any(f => f.Parent == n);
        args.Children.Template = NodeTemplate;
    }

    private IEnumerable<Node> GetChildrenForNode(Node node)
    {
        var children = new List<Node>();
        children.AddRange(DatabaseService.GetFolders().Where(f => f.Parent == node));
        children.AddRange(DatabaseService.GetComputersByFolderId(node.NodeId));
        
        return children;
    }

    private readonly RenderFragment<RadzenTreeItem> NodeTemplate = (context) => builder =>
    {
        var icon = context.Value is Computer ? "desktop_windows" : "folder";
        var name = context.Value is Computer computer ? computer.Name : (context.Value as Folder)?.Name;

        builder.OpenComponent<RadzenIcon>(0);
        builder.AddAttribute(1, "Icon", icon);
        builder.CloseComponent();
        builder.AddContent(2, $" {name}");
    };

    private string GetTextForNode(object data) => data as string;

    private async Task OnTreeChange(TreeEventArgs args)
    {
        _selectedComputers.Clear();
        _anyComputerSelected = false;
    
        var node = args.Value as Node;

        if (node is Folder)
        {
            _selectedNode = node;
            await UpdateComputersThumbnailsAsync(node.Children.OfType<Computer>());
        }

        StateHasChanged();
    }

    private async Task UpdateComputersThumbnailsAsync(IEnumerable<Computer> computers)
    {
        foreach (var computer in computers)
        {
            var clientContext = ConnectionManager.Connect("Client", $"http://{computer.IPAddress}:5076/hubs/control", true);

            try
            {
                clientContext.On<byte[]>("ReceiveThumbnail", async (thumbnailBytes) =>
                {
                    if (thumbnailBytes?.Length > 0)
                    {
                        computer.Thumbnail = thumbnailBytes;
                        await InvokeAsync(StateHasChanged);
                    }
                });

                await clientContext.StartAsync();

                var proxy = clientContext.Connection.CreateHubProxy<IControlHub>();
                await proxy.ConnectAs(Intention.GetThumbnail);
            }
            catch {}
        }
    }
    
    private async Task HandleRefreshClick()
    {
        if (_selectedNode is Folder selectedFolder)
        {
            await UpdateComputersThumbnailsAsync(selectedFolder.Children.OfType<Computer>());
        }
    }

    private async Task OpenWindow(string url)
    {
        await JSRuntime.InvokeVoidAsync("openNewWindow", url);
    }

    private async Task Control()
    {
        var tasks = _selectedComputers.Select(computer => IsComputerAvailable(computer.IPAddress)).ToArray();
        var results = await Task.WhenAll(tasks);

        foreach (var (ipAddress, isAvailable) in results)
        {
            if (isAvailable)
            {
                await OpenWindow($"/{ipAddress}/control");
            }
        }
    }

    private async Task OpenShell(RadzenSplitButtonItem item)
    {
        if (item != null)
        {
            var sParameter = item.Text.Contains("System") ? "-s" : "";
            var tasks = _selectedComputers.Select(computer => IsComputerAvailable(computer.IPAddress)).ToArray();
            var results = await Task.WhenAll(tasks);

            foreach (var (ipAddress, isAvailable) in results)
            {
                if (isAvailable)
                {
                    var command = @$"/C psexec \\{ipAddress} {sParameter} -nobanner -accepteula {item.Value}";

                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = "cmd.exe",
                        Arguments = command,
                        UseShellExecute = true,
                    };

                    await Task.Run(() => Process.Start(startInfo));
                }
            }
        }
    }

    private static async Task<(string ipAddress, bool isAvailable)> IsComputerAvailable(string ipAddress)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 1000);

            return (ipAddress, reply.Status == IPStatus.Success);
        }
        catch
        {
            return (ipAddress, false);
        }
    }
}
