// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.FluentUI.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

#pragma warning disable CA2227

namespace RemoteMaster.Server.Components.Dialogs;

public partial class PowerDialog
{
    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    [Parameter]
    public Dictionary<Computer, HubConnection> Content { get; set; } = default!;

    [Inject]
    public IComputerCommandService ComputerCommandService { get; set; } = default!;

    [Inject]
    private IWakeOnLanService WakeOnLanService { get; set; } = default!;

    private string _selectedOption;

    private async Task Confirm()
    {
        if (_selectedOption == "shutdown")
        {
            await ComputerCommandService.Execute(Content, async (computer, connection) => await connection.InvokeAsync("SendShutdownComputer", "", 0, true));
        }
        else if (_selectedOption == "reboot")
        {
            await ComputerCommandService.Execute(Content, async (computer, connection) => await connection.InvokeAsync("SendRebootComputer", "", 0, true));

        }
        else if (_selectedOption == "wakeup")
        {
            foreach (var (computer, connection) in Content)
            {
                WakeOnLanService.WakeUp(computer.MACAddress);
            }
        }

        await Dialog.CloseAsync();
    }
}
