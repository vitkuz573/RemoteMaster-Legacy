// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using MudBlazor;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class OpenShellDialog
{
    private Shell _selectedShell = Shell.Cmd;
    private string _selectedUser = "CurrentUser";
    private bool _isConnecting;
    private string _connectButtonText = "Connect";

    private async Task Connect()
    {
        _isConnecting = true;
        _connectButtonText = "Connecting...";

        StateHasChanged();

        try
        {
            await HostCommandService.Execute(Hosts, async (host, _) =>
            {
                var sParameter = _selectedUser == "System" ? "-s" : "";
                var command = _selectedShell switch
                {
                    Shell.Cmd => @$"/C psexec \\{host.IpAddress} {sParameter} -nobanner -accepteula cmd",
                    Shell.PowerShell => @$"/C psexec \\{host.IpAddress} {sParameter} -nobanner -accepteula powershell",
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
            // ignored
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
