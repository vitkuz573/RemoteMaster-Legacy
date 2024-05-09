// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library;

public partial class Button
{
    [Parameter]
    public string Label { get; set; } = "Button";

    [Parameter]
    public string CssClasses { get; set; } = "rounded bg-blue-600 px-4 py-2 font-semibold text-white hover:bg-blue-700";

    [Parameter]
    public EventCallback OnClick { get; set; }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync();
    }
}
