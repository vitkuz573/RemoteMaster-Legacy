// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Enums;

namespace RemoteMaster.Server.Components.Library;

public partial class ThemeProvider
{
    /// <summary>
    /// Determines whether the dark theme is enabled.
    /// </summary>
    [Parameter]
    public bool IsDarkTheme
    {
        get => ThemeService.IsDarkTheme;
        set => ThemeService.IsDarkTheme = value;
    }

    /// <summary>
    /// Child content that receives the theme parameter.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    protected Theme Theme => ThemeService.Theme;
}