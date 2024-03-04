// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class MessageBoxDialog
{
    private string _caption;
    private string _text;

    protected async Task Show()
    {
        foreach (var (_, connection) in Hosts)
        {
            var content = new StringBuilder();
            content.Append("Add-Type -AssemblyName System.Windows.Forms;");
            content.AppendFormat("$text = \"{0}\"; $caption = \"{1}\";", _text.Replace("\"", "`\""), _caption.Replace("\"", "`\""));
            content.Append("[System.Windows.Forms.MessageBox]::Show($text, $caption, [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)");

            var scriptExecutionRequest = new ScriptExecutionRequest(content.ToString(), Shell.PowerShell);

            if (connection != null)
            {
                await connection.InvokeAsync("SendScript", scriptExecutionRequest);
            }
        }
    }
}