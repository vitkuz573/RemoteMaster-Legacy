// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Utilities;

namespace RemoteMaster.Server.Components.Library;

public partial class ExpandablePanel
{
    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public string Icon { get; set; } = "chevron_right";

    [Parameter]
    public bool StartExpanded { get; set; } = false;

    [Parameter]
    public bool IsDisabled { get; set; } = false;

    [Parameter]
    public string CustomClass { get; set; }

    [Parameter]
    public string AriaLabel { get; set; }

    [Parameter]
    public int AnimationDurationMs { get; set; } = 300;

    [Parameter]
    public RenderFragment HeaderContent { get; set; }

    private bool IsExpanded { get; set; }

    protected override void OnInitialized()
    {
        IsExpanded = StartExpanded;
    }

    private void Toggle()
    {
        if (!IsDisabled)
        {
            IsExpanded = !IsExpanded;
        }
    }

    private string GetPanelClasses()
    {
        var builder = new CssClassBuilder()
            .AddBase(CustomClass ?? "")
            .AddBase("mb-4")
            .Add("cursor-pointer", !IsDisabled);

        return builder.Build();
    }

    private string GetIconStyles()
    {
        var builder = new CssStyleBuilder()
            .Add("transition-duration", $"{AnimationDurationMs}ms")
            .Add("transform", IsExpanded ? "rotate(90deg)" : "rotate(0deg)");

        return builder.Build();
    }

    private string GetContentStyles()
    {
        var builder = new CssStyleBuilder()
            .Add("transition-duration", $"{AnimationDurationMs}ms");

        return builder.Build();
    }
}

