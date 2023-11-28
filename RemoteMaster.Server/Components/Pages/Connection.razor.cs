// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Pages;

public partial class Connection
{
    [Parameter]
    public string Host { get; set; } = default!;

    private bool _drawerOpen = false;

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}
