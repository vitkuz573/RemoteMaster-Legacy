// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class BootToWimDialog
{
    private string _wimFile = string.Empty;

    private async Task Boot()
    {
        try
        {
            var scriptBuilder = new StringBuilder();

            var scriptExecutionRequest = new ScriptExecutionRequest(scriptBuilder.ToString(), Shell.Cmd);

            await HostCommandService.Execute(Hosts, async (_, connection) => await connection!.InvokeAsync("ExecuteScript", scriptExecutionRequest));
        }
        catch (Exception)
        {
            // ignored
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
