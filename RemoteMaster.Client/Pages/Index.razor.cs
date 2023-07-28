using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private IJSRuntime JSRuntime { get; set; }

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
            // SnackBarType = BitSnackBarType.Success;
            // SnackBarBody = "Fetch has been completed successfully";

            var adNodes = new ObservableCollection<Node>(domainComputers.Select(ou =>
            {
                var folder = new Folder(ou.Key);

                foreach (var computer in ou.Value)
                {
                    folder.Children.Add(computer);
                }

                return (Node)folder;
            }).ToList());

            // _addComputerFromADRef.Show(adNodes);
        }
        catch (Exception e)
        {
            // SnackBarType = BitSnackBarType.Error;
            // SnackBarBody = $"An error occurred during fetch: {e.Message}";
        }

        // await _snackBar.Show(SnackBarTitle, SnackBarBody, SnackBarType);
    }

    public async Task OpenNewFolder()
    {
        await DialogService.OpenAsync<NewFolderPage>("New Folder", options: new DialogOptions
        {
            Draggable = true
        });
    }

    private async Task OpenInNewTab(Computer computer)
    {
        var url = $"http://localhost:5254/{computer.IPAddress}/control";
        await JSRuntime.InvokeVoidAsync("openInNewTab", url);
    }

    private static async Task OpenShell(Computer computer)
    {
        var command = $"/C psexec \\\\{computer.IPAddress} -s powershell";

        var startInfo = new ProcessStartInfo()
        {
            FileName = "cmd.exe",
            Arguments = command,
            UseShellExecute = true,
        };

        await Task.Run(() => Process.Start(startInfo));
    }
}
