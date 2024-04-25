// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class PowerDialog
{
    private string _selectedOption = "shutdown";

    private async Task Confirm()
    {
        var powerActionRequest = new PowerActionRequest()
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        await EnsureConnectionsInitialized();

        switch (_selectedOption)
        {
            case "shutdown":
                await ComputerCommandService.Execute(Hosts, async (_, connection) => await connection.InvokeAsync("SendShutdownComputer", powerActionRequest));
                break;
            case "reboot":
                await ComputerCommandService.Execute(Hosts, async (_, connection) => await connection.InvokeAsync("SendRebootComputer", powerActionRequest));
                break;
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task EnsureConnectionsInitialized()
    {
        var tasks = Hosts.Select(async kvp =>
        {
            var (computer, connection) = kvp;

            try
            {
                if (connection != null)
                {
                    await connection.StartAsync();
                }
            }
            catch
            {
                connection = null;
            }
        });

        await Task.WhenAll(tasks);
    }
}
