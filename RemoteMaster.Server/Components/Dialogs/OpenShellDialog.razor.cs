// OpenShellDialog.razor.cs
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs
{
    public partial class OpenShellDialog
    {
        [Inject]
        private IComputerCommandService ComputerCommandService { get; set; } = default!;

        private Shell _selectedShell;
        private string _selectedUser;
        private bool _isConnecting = false;
        private string _connectButtonText = "Connect";

        private async Task Connect()
        {
            _isConnecting = true;
            _connectButtonText = "Connecting...";
            StateHasChanged(); // Update the UI to reflect the connecting state

            try
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

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = command,
                        UseShellExecute = true,
                    };

                    await Task.Run(() => Process.Start(startInfo));
                });
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the connect process
                // You might want to log this exception or notify the user
            }
            finally
            {
                _isConnecting = false;
                _connectButtonText = "Connect";
                StateHasChanged(); // Revert the UI back to the default state
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}
