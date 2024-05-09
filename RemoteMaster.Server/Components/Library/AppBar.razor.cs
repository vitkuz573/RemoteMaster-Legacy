// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library;

public partial class AppBar
{
    /// <summary>
    /// Title text displayed on the AppBar.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Application Name";

    /// <summary>
    /// Optional left-side content of the AppBar (e.g., navigation links, extra buttons).
    /// </summary>
    [Parameter]
    public RenderFragment LeftContent { get; set; }

    /// <summary>
    /// Optional right-side content of the AppBar (e.g., action buttons, search).
    /// </summary>
    [Parameter]
    public RenderFragment RightContent { get; set; }

    /// <summary>
    /// URL for the profile image/avatar.
    /// </summary>
    [Parameter]
    public string ProfileImageUrl { get; set; }
}
