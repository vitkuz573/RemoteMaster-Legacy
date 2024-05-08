// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library;

public partial class Sidebar
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public string Title { get; set; } = "Settings";

    [Parameter]
    public string IconOpen { get; set; } = "chevron_right";

    [Parameter]
    public string IconClosed { get; set; } = "chevron_left";

    /// <summary>
    /// Base color class used for both the sidebar and its toggle switch.
    /// </summary>
    [Parameter]
    public string BaseColorClass { get; set; } = "bg-gray-800 text-white";

    [Parameter]
    public int WidthPx { get; set; } = 256;

    [Parameter]
    public bool StartOpen { get; set; } = false;

    [Parameter]
    public bool IsDisabled { get; set; } = false;

    [Parameter]
    public int AnimationDurationMs { get; set; } = 500;

    /// <summary>
    /// Opacity of the toggle switch when the sidebar is open, ranging from 0 (fully transparent) to 1 (fully opaque).
    /// </summary>
    [Parameter]
    public double SwitchOpacityOpen { get; set; } = 1.0;

    /// <summary>
    /// Opacity of the toggle switch when the sidebar is closed, ranging from 0 (fully transparent) to 1 (fully opaque).
    /// </summary>
    [Parameter]
    public double SwitchOpacityClosed { get; set; } = 0.5;

    private bool _isSidebarOpen;

    protected override void OnInitialized()
    {
        _isSidebarOpen = StartOpen;
    }

    private void ToggleSidebar()
    {
        if (!IsDisabled)
        {
            _isSidebarOpen = !_isSidebarOpen;
        }
    }

    /// <summary>
    /// Gets the appropriate switch opacity based on the sidebar's current state.
    /// </summary>
    private string GetSwitchOpacity()
    {
        return (_isSidebarOpen ? SwitchOpacityOpen : SwitchOpacityClosed).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
