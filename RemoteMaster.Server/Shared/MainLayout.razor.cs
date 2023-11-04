// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MudBlazor;

namespace RemoteMaster.Server.Shared;

public partial class MainLayout
{
    private bool _isDarkMode = false;

    private readonly MudTheme _theme = new()
    {
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthRight = "300px"
        }
    };
}
