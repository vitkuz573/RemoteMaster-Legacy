using Blazorise.Snackbar;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RemoteMaster.Client.Components;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace RemoteMaster.Client.Pages;

public partial class Index
{
    private readonly ObservableCollection<Node> _nodes = new();
    private IList<Node> _expandedNodes = new List<Node>();
    private Node _selectedNode;

    private AddFolderModal _addFolderModalRef;
    private AddComputerModal _addComputerModalRef;
    private SyncResultsModal _syncResultsModalRef;

    private string _fetchComputersFromADStatus;

    private Snackbar _fetchComputersFromADStatusSnackbar;

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private ActiveDirectoryService ActiveDirectoryService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var folders = DatabaseService.GetFolders();

        foreach (var folder in folders)
        {
            _nodes.Add(folder);
        }
    }

    private async Task SyncComputersFromAD()
    {
        try
        {
            var domainComputers = await ActiveDirectoryService.FetchComputers();
            _fetchComputersFromADStatus = "Fetch has been completed successfully";

            var adNodes = new ObservableCollection<Node>();

            foreach (var ou in domainComputers)
            {
                var folder = new Folder(ou.Key);

                foreach (var computer in ou.Value)
                {
                    folder.Children.Add(computer);
                }

                adNodes.Add(folder);
            }

            _syncResultsModalRef.Show(adNodes);
        }
        catch (Exception e)
        {
            _fetchComputersFromADStatus = $"An error occurred during fetch: {e.Message}";
        }

        _fetchComputersFromADStatusSnackbar.Show();
    }

    private async Task OpenInNewTab(Computer computer)
    {
        var url = $"http://localhost:5254/{computer.IPAddress}/control";
        await JSRuntime.InvokeVoidAsync("openInNewTab", url);
    }

    private static async Task OpenShell(Computer computer, Shell shell)
    {
        var command = $"/C psexec \\\\{computer.IPAddress} -s {shell}";

        var startInfo = new ProcessStartInfo()
        {
            FileName = "cmd.exe",
            Arguments = command,
            UseShellExecute = true,
        };

        await Task.Run(() => Process.Start(startInfo));
    }
}
