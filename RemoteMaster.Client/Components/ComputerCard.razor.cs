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

    private void ShowContextMenu(MouseEventArgs args)
    {
        ContextMenuService.Open(args,
            new List<ContextMenuItem> {
            new ContextMenuItem(){ Text = "Open Command", Value = "Command", Icon = "terminal" },
            new ContextMenuItem(){ Text = "Open in Tab", Value = "Tab", Icon = "link" },
            }, OnMenuItemClick);
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
            await JSRuntime.InvokeVoidAsync("open", $"http://127.0.0.1:5254/{Computer.IPAddress}/control", "_blank");
        }

        ContextMenuService.Close();
    }
}