// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class WakeUpDialog
{
    private void Confirm()
    {
        foreach (var (host, _) in Hosts)
        {
            WakeOnLanService.WakeUp(host.MacAddress);
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
