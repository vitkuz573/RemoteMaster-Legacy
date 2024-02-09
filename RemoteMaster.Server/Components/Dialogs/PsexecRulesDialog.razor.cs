// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class PsexecRulesDialog
{
    private bool _selectedOption;

    private async Task Ok()
    {
        await ComputerCommandService.Execute(Hosts, async (_, connection) => await connection.InvokeAsync("SetPSExecRules", _selectedOption));

        MudDialog.Close(DialogResult.Ok(true));
    }
}
