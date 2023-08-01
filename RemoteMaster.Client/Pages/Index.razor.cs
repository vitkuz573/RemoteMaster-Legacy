using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;

namespace RemoteMaster.Client.Pages;

public partial class Index
{
    private List<Node> _entries;

    private Node _selectedNode;

    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private ActiveDirectoryService ActiveDirectoryService { get; set; }

    protected override void OnInitialized()
    {
        _entries = new List<Node>();

        var folders = DatabaseService.GetFolders();

        foreach (var folder in folders)
        {
            _entries.Add(folder);
        }

        DatabaseService.NodeAdded += OnNodeAdded;
    }

    private void OnNodeAdded(object? sender, Node node)
    {
        if (node is Folder folder)
        {
            _entries.Add(folder);
        }

        StateHasChanged();
    }

    private async void LoadComputers(TreeExpandEventArgs args)
    {
        var node = args.Value as Node;
        var nodeId = node.NodeId;

        var computers = DatabaseService.GetComputersByFolderId(nodeId);

        args.Children.Data = computers;
        args.Children.Text = GetTextForNode;
        args.Children.HasChildren = (node) => false;
        args.Children.Template = ComputerTemplate;
    }

    private RenderFragment<RadzenTreeItem> ComputerTemplate = (context) => builder =>
    {
        if (context.Value is Computer computer)
        {
            builder.OpenComponent<RadzenIcon>(0);
            builder.AddAttribute(1, "Icon", "desktop_windows");
            builder.CloseComponent();

            builder.AddContent(2, $" {computer.Name} ({computer.IPAddress})");
        }
        else if (context.Value is Folder folder)
        {
            builder.OpenComponent<RadzenIcon>(0);
            builder.AddAttribute(1, "Icon", "folder");
            builder.CloseComponent();

            builder.AddContent(2, $" {folder.Name}");
        }
    };

    private string GetTextForNode(object data) => data as string;

    private async Task GetComputersFromAD()
    {
        try
        {
            var domainComputers = await ActiveDirectoryService.FetchComputers();

            var adNodes = new ObservableCollection<Node>(domainComputers.Select(ou =>
            {
                var folder = new Folder(ou.Key);

                foreach (var computer in ou.Value)
                {
                    folder.Children.Add(computer);
                }

                return (Node)folder;
            }).ToList());
        }
        catch { }
    }

    public async Task OpenNewFolder()
    {
        await DialogService.OpenAsync<NewFolderPage>("New Folder", options: new DialogOptions
        {
            Draggable = true
        });
    }

    public async Task OpenNewComputer()
    {
        await DialogService.OpenAsync<NewComputerPage>("New Computer", options: new DialogOptions
        {
            Draggable = true
        });
    }

    private void OnTreeChange(TreeEventArgs args)
    {
        var node = args.Value as Node;

        if (node is Folder)
        {
            _selectedNode = node;
        }

        StateHasChanged();
    }
}
