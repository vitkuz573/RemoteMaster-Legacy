// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library;

public partial class Sidebar
{
    /// <summary>
    /// The content to be displayed inside the sidebar, represented as a render fragment.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Title text to be displayed at the top of the sidebar.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Settings";

    /// <summary>
    /// Icon to be shown when the sidebar is open, usually an arrow or similar indicator.
    /// </summary>
    [Parameter]
    public string IconOpen { get; set; } = "chevron_right";

    /// <summary>
    /// Icon to be shown when the sidebar is closed, indicating its current state.
    /// </summary>
    [Parameter]
    public string IconClosed { get; set; } = "chevron_left";

    /// <summary>
    /// Base color class used for both the sidebar and its toggle switch.
    /// Defines the primary styles such as background and text colors.
    /// </summary>
    [Parameter]
    public string BaseColorClass { get; set; } = "bg-gray-800 text-white";

    /// <summary>
    /// Width of the sidebar in pixels.
    /// Defines the width of the entire sidebar component.
    /// </summary>
    [Parameter]
    public int WidthPx { get; set; } = 256;

    /// <summary>
    /// Specifies whether the sidebar should start in an open state when first rendered.
    /// </summary>
    [Parameter]
    public bool StartOpen { get; set; } = false;

    /// <summary>
    /// Indicates if the sidebar's toggle function is disabled, preventing it from being opened or closed.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; } = false;

    /// <summary>
    /// Duration of the sidebar open/close animation in milliseconds.
    /// Defines the animation speed when toggling the sidebar.
    /// </summary>
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

    /// <summary>
    /// Event callback invoked when the sidebar is toggled (opened or closed).
    /// Provides the current open state as a boolean parameter.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnToggle { get; set; }

    private bool _isSidebarOpen;

    protected override void OnInitialized()
    {
        _isSidebarOpen = StartOpen;
    }

    private async Task ToggleSidebar()
    {
        if (!IsDisabled)
        {
            _isSidebarOpen = !_isSidebarOpen;
            await OnToggle.InvokeAsync(_isSidebarOpen);
        }
    }

    private string GetSwitchOpacity()
    {
        return (_isSidebarOpen ? SwitchOpacityOpen : SwitchOpacityClosed).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
