// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using MudBlazor;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class OpenShellDialog
{
    private Shell _selectedShell;
    private string _selectedUser;
    private bool _isConnecting = false;
    private string _connectButtonText = "Connect";

    public OpenShellDialog()
    {
        _selectedShell = Shell.Cmd;
        _selectedUser = "CurrentUser";
    }

    private async Task Connect()
    {
        _isConnecting = true;
        _connectButtonText = "Connecting...";

        StateHasChanged();

        try
        {
            await ComputerCommandService.Execute(Hosts, async (computer, connection) =>
            {
                var sParameter = _selectedUser == "System" ? "-s" : "";
                var command = _selectedShell switch
                {
                    Shell.Cmd => @$"/C psexec \\{computer.IpAddress} {sParameter} -nobanner -accepteula cmd",
                    Shell.PowerShell => @$"/C psexec \\{computer.IpAddress} {sParameter} -nobanner -accepteula powershell",
                    _ => throw new InvalidOperationException($"Unknown shell: {_selectedShell}")
                };

                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = command,
                    UseShellExecute = true,
                };

                await Task.Run(() => Process.Start(startInfo));
            });
        }
        catch (Exception)
        {
        }
        finally
        {
            _isConnecting = false;
            _connectButtonText = "Connect";
            StateHasChanged();
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
