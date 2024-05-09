// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Enums;

namespace RemoteMaster.Server.Components.Library;

public partial class Button
{
    /// <summary>
    /// Text displayed on the button.
    /// </summary>
    [Parameter]
    public string Label { get; set; } = "Button";

    /// <summary>
    /// Additional CSS classes for styling the button using Tailwind CSS.
    /// </summary>
    [Parameter]
    public string CssClasses { get; set; } = "rounded-lg bg-blue-600 px-4 py-2 font-semibold text-white hover:bg-blue-700";

    /// <summary>
    /// Indicates if the button is disabled.
    /// </summary>
    [Parameter]
    public bool IsDisabled { get; set; } = false;

    /// <summary>
    /// Icon displayed to the left of the button text.
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = "";

    /// <summary>
    /// Specifies the button type (submit, reset, button).
    /// </summary>
    [Parameter]
    public ButtonType ButtonType { get; set; } = ButtonType.Button;

    /// <summary>
    /// Event callback triggered on button click.
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>
    /// Handles button click if the button is not disabled.
    /// </summary>
    private async Task HandleClick()
    {
        if (!IsDisabled)
        {
            await OnClick.InvokeAsync();
        }
    }

    private string GetButtonClasses()
    {
        var classes = CssClasses;

        if (IsDisabled)
        {
            classes += " bg-gray-400 hover:bg-gray-400 border-gray-500 cursor-not-allowed opacity-50";
        }

        return classes;
    }

    private string GetButtonTypeAsString()
    {
        return ButtonType.ToString().ToLower();
    }
}
