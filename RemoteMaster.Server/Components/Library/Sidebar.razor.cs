// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

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

    [Parameter]
    public string CustomClass { get; set; } = "bg-gray-800 text-white";

    [Parameter]
    public int WidthPx { get; set; } = 256;

    [Parameter]
    public bool StartOpen { get; set; } = false;

    [Parameter]
    public bool IsDisabled { get; set; } = false;

    [Parameter]
    public int AnimationDurationMs { get; set; } = 500;

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
}
