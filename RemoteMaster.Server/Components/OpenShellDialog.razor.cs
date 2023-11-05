// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

#pragma warning disable CA2227

public partial class OpenShellDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }

    [Parameter]
    public Dictionary<Computer, HubConnection> Hosts { get; set; }

    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    private string _selectedShell;
    private string _selectedUser;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Connect()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) =>
        {
            ProcessStartInfo startInfo;

            if (_selectedShell == "ssh")
            {
                var command = $"ssh user@{computer.IPAddress}";

                startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    UseShellExecute = true,
                };
            }
            else
            {
                var sParameter = _selectedUser == "system" ? "-s" : "";
                var command = @$"/C psexec \\{computer.IPAddress} {sParameter} -nobanner -accepteula {_selectedShell}";

                startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = command,
                    UseShellExecute = true,
                };
            }

            await Task.Run(() => Process.Start(startInfo));
        });

        MudDialog.Close(DialogResult.Ok(true));
    }
}
