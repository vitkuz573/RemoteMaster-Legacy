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
    private IWakeOnLanService WakeOnLanService { get; set; }

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
        var tasks = computers.Select(UpdateComputerThumbnailAsync).ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task UpdateComputerThumbnailAsync(Computer computer)
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

    private async Task ExecuteOnAvailableComputers(Func<Computer, IControlHub, Task> actionOnComputer)
    {
        var tasks = _selectedComputers.Select(IsComputerAvailable).ToArray();
        var results = await Task.WhenAll(tasks);

        var availableComputers = results.Where(r => r.isAvailable);

        foreach (var (computer, isAvailable) in availableComputers)
        {
            var clientContext = ConnectionManager.Connect("Client", $"http://{computer.IPAddress}:5076/hubs/control", true);
            await clientContext.StartAsync();

            var proxy = clientContext.Connection.CreateHubProxy<IControlHub>();
            await actionOnComputer(computer, proxy);
        }

        StateHasChanged();
    }

    private async Task Control()
    {
        await ExecuteOnAvailableComputers(async (computer, proxy) => await OpenWindow($"/{computer.IPAddress}/control"));
    }

    private async Task OpenShell(RadzenSplitButtonItem item)
    {
        if (item == null)
        {
            return;
        }

        var sParameter = item.Text.Contains("System") ? "-s" : "";

        await ExecuteOnAvailableComputers(async (computer, proxy) =>
        {
            var command = @$"/C psexec \\{computer.IPAddress} {sParameter} -nobanner -accepteula {item.Value}";

            var startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = command,
                UseShellExecute = true,
            };

            await Task.Run(() => Process.Start(startInfo));
        });
    }

    private async Task Power(RadzenSplitButtonItem item)
    {
        if (item == null)
        {
            return;
        }

        await ExecuteOnAvailableComputers(async (computer, proxy) =>
        {
            if (item.Value == "shutdown")
            {
                // shutdown logic
            }

            if (item.Value == "reboot")
            {
                await proxy.RebootComputer("", 0, true);
            }
        });
    }

    private async Task Update()
    {
        await ExecuteOnAvailableComputers(async (computer, proxy) =>
        {
            var url = $"http://{computer.IPAddress}:5124/api/Update/update";

            using var client = new HttpClient();

            try
            {
                var response = await client.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        });
    }

    private async Task StartMassRecording()
    {
        await ExecuteOnAvailableComputers(async (computer, proxy) =>
        {
            var requesterName = Environment.MachineName;
            var currentDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $@"C:\{requesterName}_{computer.IPAddress}_{currentDate}.mp4";

            await proxy.StartScreenRecording(fileName);
        });
    }

    private async Task StopMassRecording()
    {
        await ExecuteOnAvailableComputers(async (computer, proxy) => await proxy.StopScreenRecording());
    }

    private async Task Wake()
    {
        foreach (var computer in _selectedComputers)
        {
            WakeOnLanService.WakeUp(computer.MACAddress);
        }
    }

    private static async Task<(Computer computer, bool isAvailable)> IsComputerAvailable(Computer computer)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(computer.IPAddress, 1000);

            return (computer, reply.Status == IPStatus.Success);
        }
        catch
        {
            return (computer, false);
        }
    }
}
