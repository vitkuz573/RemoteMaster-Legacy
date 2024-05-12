// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Enums;
using RemoteMaster.Server.Components.Library.Extensions;
using RemoteMaster.Server.Components.Library.Utilities;

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
    public int WidthPx { get; set; } = 256;

    [Parameter]
    public bool StartOpen { get; set; } = false;

    [Parameter]
    public bool IsDisabled { get; set; } = false;

    [Parameter]
    public int AnimationDurationMs { get; set; } = 500;

    [Parameter]
    public Opacity SwitchOpacityOpen { get; set; } = Opacity.Full;

    [Parameter]
    public Opacity SwitchOpacityClosed { get; set; } = Opacity.Fifty;

    [Parameter]
    public EventCallback<bool> OnToggle { get; set; }

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

    private string GetSidebarClasses()
    {
        var builder = new CssClassBuilder()
            .AddBase("fixed top-0 h-full shadow-lg p-5 transition-all duration-500 ease-out")
            .Add("bg-gray-800 text-white", Theme == Theme.Dark)
            .Add("bg-white text-gray-900", Theme == Theme.Light)
            .Add("right-0", Position == SidebarPosition.Right)
            .Add("left-0", Position == SidebarPosition.Left);

        return builder.Build();
    }

    private string GetDynamicStyles()
    {
        var builder = new CssStyleBuilder()
            .Add("transition-duration", $"{AnimationDurationMs}ms")
            .Add("right", _isSidebarOpen ? "0" : $"-{WidthPx}px", Position == SidebarPosition.Right)
            .Add("left", _isSidebarOpen ? "0" : $"-{WidthPx}px", Position == SidebarPosition.Left)
            .Add("width", $"{WidthPx}px");

        return builder.Build();
    }

    private string GetSwitchClasses()
    {
        var builder = new CssClassBuilder()
            .AddBase("absolute inset-y-1/2 flex h-10 w-5 cursor-pointer items-center justify-center transition-opacity duration-300")
            .Add("bg-gray-800 text-white", Theme == Theme.Dark)
            .Add("bg-gray-300 text-black", Theme == Theme.Light)
            .Add("rounded-bl-full rounded-tl-full -left-5", Position == SidebarPosition.Right)
            .Add("rounded-br-full rounded-tr-full -right-5", Position == SidebarPosition.Left)
            .Add(SwitchOpacityOpen.ToCss(), _isSidebarOpen)
            .Add(SwitchOpacityClosed.ToCss(), !_isSidebarOpen);

        return builder.Build();
    }

    private string GetSwitchIcon()
    {
        return _isSidebarOpen
            ? (Position == SidebarPosition.Right ? IconOpen : IconClosed)
            : (Position == SidebarPosition.Right ? IconClosed : IconOpen);
    }
}
