// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RemoteMaster.Server.Components.Library.Enums;
using RemoteMaster.Server.Components.Library.Extensions;

namespace RemoteMaster.Server.Components.Library;

public partial class Overlay
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter]
    public string AdditionalClasses { get; set; } = string.Empty;

    [Parameter]
    public bool IsVisible { get; set; } = true;

    [Parameter]
    public BackgroundColor BackgroundColor { get; set; } = BackgroundColor.Black;

    [Parameter]
    public Opacity Opacity { get; set; } = Opacity.Fifty;

    [Parameter]
    public Position Position { get; set; } = Position.Fixed;

    [Parameter]
    public Inset Inset { get; set; } = Inset.All;

    [Parameter]
    public Display Display { get; set; } = Display.Flex;

    [Parameter]
    public JustifyContent JustifyContent { get; set; } = JustifyContent.Center;

    [Parameter]
    public AlignItems AlignItems { get; set; } = AlignItems.Center;

    private string OverlayClass => $"{Position.ToCss()} {Inset.ToCss()} {BackgroundColor.ToCss()} {Opacity.ToCss()} {Display.ToCss()} {JustifyContent.ToCss()} {AlignItems.ToCss()} {AdditionalClasses}";
}
