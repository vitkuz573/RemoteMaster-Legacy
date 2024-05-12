// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Components.Library.Enums;

namespace RemoteMaster.Server.Components.Library.Extensions;

public static class CssExtensions
{
    public static string ToCss(this BackgroundColor color) => color switch
    {
        BackgroundColor.Black => "bg-black",
        BackgroundColor.White => "bg-white",
        BackgroundColor.Gray => "bg-gray-500",
        _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
    };

    public static string ToCss(this Opacity opacity) => opacity switch
    {
        Opacity.Zero => "opacity-0",
        Opacity.TwentyFive => "opacity-25",
        Opacity.Fifty => "opacity-50",
        Opacity.SeventyFive => "opacity-75",
        Opacity.Full => "opacity-100",
        _ => throw new ArgumentOutOfRangeException(nameof(opacity), opacity, null)
    };

    public static string ToCss(this Position position) => position switch
    {
        Position.Static => "static",
        Position.Fixed => "fixed",
        Position.Absolute => "absolute",
        Position.Relative => "relative",
        Position.Sticky => "sticky",
        _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
    };

    public static string ToCss(this Inset inset) => inset switch
    {
        Inset.None => "",
        Inset.All => "inset-0",
        Inset.X => "inset-x-0",
        Inset.Y => "inset-y-0",
        Inset.Top => "top-0",
        Inset.Bottom => "bottom-0",
        Inset.Left => "left-0",
        Inset.Right => "right-0",
        _ => throw new ArgumentOutOfRangeException(nameof(inset), inset, null)
    };

    public static string ToCss(this Display display) => display switch
    {
        Display.None => "hidden",
        Display.Flex => "flex",
        Display.Block => "block",
        Display.InlineBlock => "inline-block",
        Display.Inline => "inline",
        _ => throw new ArgumentOutOfRangeException(nameof(display), display, null)
    };

    public static string ToCss(this JustifyContent justifyContent) => justifyContent switch
    {
        JustifyContent.Start => "justify-start",
        JustifyContent.End => "justify-end",
        JustifyContent.Center => "justify-center",
        JustifyContent.Between => "justify-between",
        JustifyContent.Around => "justify-around",
        _ => throw new ArgumentOutOfRangeException(nameof(justifyContent), justifyContent, null)
    };

    public static string ToCss(this AlignItems alignItems) => alignItems switch
    {
        AlignItems.Start => "items-start",
        AlignItems.End => "items-end",
        AlignItems.Center => "items-center",
        AlignItems.Stretch => "items-stretch",
        AlignItems.Baseline => "items-baseline",
        _ => throw new ArgumentOutOfRangeException(nameof(alignItems), alignItems, null)
    };

    public static string ToCss(this TransformRotation rotation) => rotation switch
    {
        TransformRotation.None => "",
        TransformRotation.Rotate90 => "rotate-90",
        TransformRotation.Rotate180 => "rotate-180",
        TransformRotation.Rotate270 => "rotate-270",
        TransformRotation.Rotate360 => "rotate-360",
        _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
    };
}
