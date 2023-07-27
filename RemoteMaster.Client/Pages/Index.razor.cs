using System.Collections.ObjectModel;
using System.Diagnostics;
using Bit.BlazorUI;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RemoteMaster.Client.Components;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;

namespace RemoteMaster.Client.Pages;

public partial class Index
{
    private List<BitNavItem> _nodes = new();
    private Node _selectedNode;
    private AddFolder _addFolderRef;
    private AddComputerManual _addComputerManualRef;
    private AddComputerFromAD _addComputerFromADRef;
    private BitSnackBar _snackBar = new();
    private BitSnackBarPosition SnackBarPosition = BitSnackBarPosition.BottomCenter;
    private string SnackBarTitle = string.Empty;
    private string? SnackBarBody;
    private bool SnackBarAutoDismiss = true;
    private int SnackBarDismissSeconds = 5;
    private BitSnackBarType SnackBarType;
    private Dictionary<string, Node> _nodeLookup = new();

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private ActiveDirectoryService ActiveDirectoryService { get; set; }

    protected override void OnInitialized()
    {
        var folders = DatabaseService.GetFolders();

        _nodes = folders.Select(f => CreateBitNavItem(f)).ToList();
    }

    private BitNavItem CreateBitNavItem(Node node)
    {
        if (node is Folder folder)
        {
            var key = folder.NodeId.ToString();
            _nodeLookup.Add(key, folder);

            return new BitNavItem
            {
                Key = key,
                Text = folder.Name,
                IconName = BitIconName.Folder,
                ChildItems = folder.Children.Select(c => CreateBitNavItem(c)).ToList()
            };
        }
        else if (node is Computer computer)
        {
            var key = computer.NodeId.ToString();
            _nodeLookup.Add(key, computer);

            return new BitNavItem
            {
                Key = key,
                Text = computer.Name,
                IconName = BitIconName.ScreenCast
            };
        }

        throw new ArgumentException("Unsupported node type");
    }

    private async Task GetComputersFromAD()
    {
        try
        {
            var domainComputers = await ActiveDirectoryService.FetchComputers();
            SnackBarType = BitSnackBarType.Success;
            SnackBarBody = "Fetch has been completed successfully";

            var adNodes = new ObservableCollection<Node>(domainComputers.Select(ou =>
            {
                var folder = new Folder(ou.Key);

                foreach (var computer in ou.Value)
                {
                    folder.Children.Add(computer);
                }

                return (Node)folder;
            }).ToList());

            _addComputerFromADRef.Show(adNodes);
        }
        catch (Exception e)
        {
            SnackBarType = BitSnackBarType.Error;
            SnackBarBody = $"An error occurred during fetch: {e.Message}";
        }

        await _snackBar.Show(SnackBarTitle, SnackBarBody, SnackBarType);
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

    private void OnItemClick(BitNavItem item)
    {
        _selectedNode = _nodeLookup[item.Key];
        StateHasChanged();
    }
}
