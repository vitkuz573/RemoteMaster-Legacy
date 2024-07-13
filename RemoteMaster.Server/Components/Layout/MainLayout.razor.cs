// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MudBlazor;

namespace RemoteMaster.Server.Components.Layout;

public partial class MainLayout
{
    private readonly MudTheme _theme = new()
    {
        LayoutProperties = new LayoutProperties
        {
            DrawerWidthRight = "420px",
            AppbarHeight = "55px"
        },
        PaletteLight = new PaletteLight
        {
            Primary = "#4F46E5"
        },
    };
}
