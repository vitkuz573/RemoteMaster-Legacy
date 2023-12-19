// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class PowerDialog
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; } = default!;

    [Inject]
    private IWakeOnLanService WakeOnLanService { get; set; } = default!;

    private string _selectedOption;

    private async Task Confirm()
    {
        if (_selectedOption == "shutdown")
        {
            await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendShutdownComputer", "", 0, true));
        }
        else if (_selectedOption == "reboot")
        {
            await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendRebootComputer", "", 0, true));

        }
        else if (_selectedOption == "wakeup")
        {
            foreach (var (computer, connection) in Hosts)
            {
                WakeOnLanService.WakeUp(computer.MACAddress);
            }
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
