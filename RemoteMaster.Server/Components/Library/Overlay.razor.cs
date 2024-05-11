// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace RemoteMaster.Server.Components.Library;

public partial class Overlay
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private string OverlayClass => $"fixed inset-0 bg-black bg-opacity-50 flex justify-center items-center {AdditionalClasses}";

    [Parameter]
    public string AdditionalClasses { get; set; } = string.Empty;
}
