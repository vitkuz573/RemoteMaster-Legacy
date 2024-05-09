// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Enums;

namespace RemoteMaster.Server.Components.Library;

public partial class Button
{
    [Parameter]
    public string Label { get; set; } = "Button";

    [Parameter]
    public string CssClasses { get; set; } = "rounded-lg bg-blue-600 px-4 py-2 font-semibold text-white hover:bg-blue-700";

    [Parameter]
    public bool IsDisabled { get; set; } = false;

    [Parameter]
    public string Icon { get; set; } = "";

    [Parameter]
    public string IconClasses { get; set; } = "material-icons mr-2";

    [Parameter]
    public ButtonType ButtonType { get; set; } = ButtonType.Button;

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public bool IsToggled { get; set; } = false;

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
            classes += " bg-gray-400 hover:bg-gray-400 cursor-not-allowed opacity-50";
        }

        return classes;
    }

    private string GetButtonTypeAsString() => ButtonType.ToString().ToLower();
}
