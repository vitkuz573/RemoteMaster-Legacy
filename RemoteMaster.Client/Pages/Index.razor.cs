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

    private AddFolder _addFolderRef;
    private AddComputerManual _addComputerManualRef;
    private AddComputerFromAD _addComputerFromADRef;

    private string _getComputersFromADStatus;
    private Snackbar _getComputersFromADStatusSnackbar;

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

    private async Task GetComputersFromAD()
    {
        try
        {
            var domainComputers = await ActiveDirectoryService.FetchComputers();
            _getComputersFromADStatus = "Fetch has been completed successfully";

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

            _addComputerFromADRef.Show(adNodes);
        }
        catch (Exception e)
        {
            _getComputersFromADStatus = $"An error occurred during fetch: {e.Message}";
        }

        _getComputersFromADStatusSnackbar.Show();
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
