// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class PowerDialog
{
    private string _selectedOption = "shutdown";

    private async Task Confirm()
    {
        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        switch (_selectedOption)
        {
            case "shutdown":
                await HostCommandService.ExecuteAsync(Hosts, async (_, connection) => await connection!.InvokeAsync("ShutdownHost", powerActionRequest));
                break;
            case "reboot":
                await HostCommandService.ExecuteAsync(Hosts, async (_, connection) => await connection!.InvokeAsync("RebootHost", powerActionRequest));
                break;
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
