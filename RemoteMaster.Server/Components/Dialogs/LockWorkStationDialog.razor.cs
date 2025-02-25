// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class LockWorkStationDialog
{
    private async Task Lock()
    {
        try
        {
            await HostCommandService.ExecuteAsync(Hosts, async (_, connection) => await connection!.InvokeAsync("LockWorkStation"));
        }
        catch (Exception)
        {
            // ignored
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
