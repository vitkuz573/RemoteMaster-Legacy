// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class PowerDialog
{
    private string _selectedOption = "shutdown";

    private async Task Confirm()
    {
        switch (_selectedOption)
        {
            case "shutdown":
                await ComputerCommandService.Execute(Hosts, async (_, connection) => await connection.InvokeAsync("SendShutdownComputer", "", 0, true));
                break;
            case "reboot":
                await ComputerCommandService.Execute(Hosts, async (_, connection) => await connection.InvokeAsync("SendRebootComputer", "", 0, true));
                break;
            case "wakeup":
            {
                foreach (var (computer, _) in Hosts)
                {
                    WakeOnLanService.WakeUp(computer.MacAddress);
                }

                break;
            }
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
