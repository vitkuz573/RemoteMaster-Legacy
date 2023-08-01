using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Components;

public partial class ComputerCard
{
    [Parameter]
    public Computer Computer { get; set; }

    [Inject]
    public IJSRuntime JSRuntime { get; set; }

    [Inject]
    public ContextMenuService ContextMenuService { get; set; }

    private readonly List<ContextMenuItem> _contextMenuItems;

    public ComputerCard()
    {
        _contextMenuItems = new List<ContextMenuItem> {
            new ContextMenuItem
            {
                Text = "Open Command",
                Value = "Command",
                Icon = "terminal"
            },
            new ContextMenuItem
            {
                Text = "Open in Tab",
                Value = "Tab",
                Icon = "link"
            },
        };
    }

    private async void ShowContextMenu(MouseEventArgs args)
    {
        await JSRuntime.InvokeVoidAsync("toggleSelection", Computer.IPAddress);
        ContextMenuService.Open(args, _contextMenuItems, OnMenuItemClick);
    }

    private async void OnMenuItemClick(MenuItemEventArgs args)
    {
        if (args.Value.Equals("Command"))
        {
            var command = $"/C psexec \\\\{Computer.IPAddress} -s powershell";

            var startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = command,
                UseShellExecute = true,
            };

            await Task.Run(() => Process.Start(startInfo));
        }
        else if (args.Value.Equals("Tab"))
        {
            await JSRuntime.InvokeVoidAsync("openTabs");
        }

        ContextMenuService.Close();
    }
}