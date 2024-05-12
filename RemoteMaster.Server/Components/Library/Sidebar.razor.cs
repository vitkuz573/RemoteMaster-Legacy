// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Enums;
using RemoteMaster.Server.Components.Library.Utilities;

namespace RemoteMaster.Server.Components.Library;

public partial class Sidebar
{
    /// <summary>
    /// The content to be displayed inside the sidebar as a render fragment.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    /// <summary>
    /// Title text to be displayed at the top of the sidebar.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Settings";

    /// <summary>
    /// Icon to be shown when the sidebar is open.
    /// </summary>
    [Parameter]
    public string IconOpen { get; set; } = "chevron_right";

    /// <summary>
    /// Icon to be shown when the sidebar is closed.
    /// </summary>
    [Parameter]
    public string IconClosed { get; set; } = "chevron_left";

    /// <summary>
    /// CSS class defining the sidebar's basic styles like borders or padding.
    /// </summary>
    [Parameter]
    public string BaseStyleClass { get; set; } = "shadow-lg p-5";

    /// <summary>
    /// Sidebar width in pixels.
    /// </summary>
    [Parameter]
    public int WidthPx { get; set; } = 256;

    /// <summary>
    /// Initial open state of the sidebar.
    /// </summary>
    [Parameter]
    public bool StartOpen { get; set; } = false;

    /// <summary>
    /// Indicates if the sidebar's toggle function is disabled.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; } = false;

    /// <summary>
    /// Animation duration in milliseconds for opening/closing the sidebar.
    /// </summary>
    [Parameter]
    public int AnimationDurationMs { get; set; } = 500;

    /// <summary>
    /// Opacity of the toggle switch when the sidebar is open.
    /// </summary>
    [Parameter]
    public double SwitchOpacityOpen { get; set; } = 1.0;

    /// <summary>
    /// Opacity of the toggle switch when the sidebar is closed.
    /// </summary>
    [Parameter]
    public double SwitchOpacityClosed { get; set; } = 0.5;

    /// <summary>
    /// Callback event invoked when the sidebar is toggled.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OnToggle { get; set; }

    /// <summary>
    /// Additional transition classes for customizing sidebar animation.
    /// </summary>
    [Parameter]
    public string TransitionClasses { get; set; } = "transition-all duration-500 ease-out";

    /// <summary>
    /// CSS classes used for the light theme of the sidebar.
    /// These classes define the background and text color for the light mode.
    /// </summary>
    [Parameter]
    public string LightThemeClass { get; set; } = "bg-white text-gray-900";

    /// <summary>
    /// CSS classes used for the dark theme of the sidebar.
    /// These classes define the background and text color for the dark mode.
    /// </summary>
    [Parameter]
    public string DarkThemeClass { get; set; } = "bg-gray-800 text-white";

    /// <summary>
    /// CSS classes used for the light theme of the toggle switch.
    /// These classes define the background and text color for the toggle switch in light mode.
    /// </summary>
    [Parameter]
    public string LightSwitchClass { get; set; } = "bg-gray-300 text-black";

    /// <summary>
    /// CSS classes used for the dark theme of the toggle switch.
    /// These classes define the background and text color for the toggle switch in dark mode.
    /// </summary>
    [Parameter]
    public string DarkSwitchClass { get; set; } = "bg-gray-800 text-white";

    /// <summary>
    /// Specifies the position of the sidebar (left or right).
    /// </summary>
    [Parameter]
    public SidebarPosition Position { get; set; } = SidebarPosition.Right;

    private Theme Theme => ThemeService.Theme;

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

    private string GetDynamicStyle()
    {
        return $"transition-duration:{AnimationDurationMs}ms; {(Position == SidebarPosition.Right ? "right" : "left")}:{(_isSidebarOpen ? "0" : $"-{WidthPx}px")}; width:{WidthPx}px";
    }

    private string GetSidebarClasses()
    {
        return new CssClassBuilder()
            .AddBase("fixed top-0 h-full")
            .Add("shadow-lg p-5")
            .Add(Theme == Theme.Dark ? "bg-gray-800 text-white" : "bg-white text-gray-900")
            .Add(Position == SidebarPosition.Right ? "right-0" : "left-0")
            .Add("transition-all duration-500 ease-out")
            .Build();
    }

    private string GetSwitchClasses()
    {
        return new CssClassBuilder()
            .AddBase("absolute inset-y-1/2 flex h-10 w-5 cursor-pointer items-center justify-center")
            .Add(Theme == Theme.Dark ? "bg-gray-800 text-white" : "bg-gray-300 text-black")
            .Add(Position == SidebarPosition.Right ? "rounded-bl-full rounded-tl-full -left-5" : "rounded-br-full rounded-tr-full -right-5")
            .Add("transition-opacity duration-300")
            .Build();
    }

    private string GetSwitchOpacity()
    {
        return (_isSidebarOpen ? SwitchOpacityOpen : SwitchOpacityClosed).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private string GetSwitchIcon()
    {
        return _isSidebarOpen
            ? (Position == SidebarPosition.Right ? IconOpen : IconClosed)
            : (Position == SidebarPosition.Right ? IconClosed : IconOpen);
    }
}
