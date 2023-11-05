// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

#pragma warning disable CA2227

public partial class PowerDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }

    [Parameter]
    public Dictionary<Computer, HubConnection> AvailableHosts { get; set; }

    [Parameter]
    public List<Computer> Hosts { get; set; }

    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    [Inject]
    private IWakeOnLanService WakeOnLanService { get; set; }

    private string _selectedOption;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Confirm()
    {
        if (_selectedOption == "power")
        {
            await ComputerCommandService.Execute(AvailableHosts, async (computer, connection) => await connection.InvokeAsync("SendShutdownComputer", "", 0, true));
        }
        else if (_selectedOption == "reboot")
        {
            await ComputerCommandService.Execute(AvailableHosts, async (computer, connection) => await connection.InvokeAsync("SendRebootComputer", "", 0, true));

        }
        else if (_selectedOption == "wakeup")
        {
            foreach (var computer in AvailableHosts.Keys)
            {
                WakeOnLanService.WakeUp(computer.MACAddress);
            }
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
