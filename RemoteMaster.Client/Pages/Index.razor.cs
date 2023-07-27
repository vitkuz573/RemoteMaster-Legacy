using Bit.BlazorUI;
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

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private DatabaseService DatabaseService { get; set; }

    [Inject]
    private ActiveDirectoryService ActiveDirectoryService { get; set; }

    protected override void OnInitialized()
    {
        var folders = DatabaseService.GetFolders();

        _nodes = folders.Select(f => new BitNavItem
        {
            Text = f.Name,
            IconName = BitIconName.Folder,
            ChildItems = f.Children
                .Select(c =>
                {
                    if (c is Computer computer)
                    {
                        return new BitNavItem
                        {
                            Text = c.Name,
                            Key = computer.IPAddress,
                            IconName = BitIconName.ScreenCast
                        };
                    }

                    return new BitNavItem
                    {
                        Text = c.Name,
                        IconName = BitIconName.Folder
                    };
                })
                .ToList()
        }).ToList();
    }

    private async Task GetComputersFromAD()
    {
        try
        {
            var domainComputers = await ActiveDirectoryService.FetchComputers();
            SnackBarType = BitSnackBarType.Success;
            SnackBarBody = "Fetch has been completed successfully";

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
        // 
    }
}
