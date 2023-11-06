// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

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

    private Shell _selectedShell;
    private string _selectedUser;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Connect()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) =>
        {
            var sParameter = _selectedUser == "system" ? "-s" : "";
            var command = _selectedShell switch
            {
                Shell.SSH => $"ssh user@{computer.IPAddress}",
                Shell.Cmd => @$"/C psexec \\{computer.IPAddress} {sParameter} -nobanner -accepteula cmd",
                Shell.PowerShell => @$"/C psexec \\{computer.IPAddress} {sParameter} -nobanner -accepteula powershell",
                _ => throw new InvalidOperationException($"Unknown shell: {_selectedShell}")
            };

            var startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = command,
                UseShellExecute = true,
            };

            await Task.Run(() => Process.Start(startInfo));
        });

        MudDialog.Close(DialogResult.Ok(true));
    }
}
