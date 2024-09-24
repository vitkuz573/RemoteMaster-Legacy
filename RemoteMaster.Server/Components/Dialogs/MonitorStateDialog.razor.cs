// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class MonitorStateDialog
{
    private MonitorState _monitorState;

    private async Task SetState()
    {
        await HostCommandService.Execute(Hosts, async (_, connection) => await connection.InvokeAsync("SetMonitorState", _monitorState));

        MudDialog.Close(DialogResult.Ok(true));
    }
}
