// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library;

public partial class Dialog : ComponentBase
{
    [CascadingParameter]
    private DialogInstance DialogInstance { get; set; }

    [CascadingParameter(Name = "IsNested")]
    private bool IsNested { get; set; }

    [Inject]
    protected IDialogWindowService DialogService { get; set; }

    [Parameter]
    public RenderFragment TitleContent { get; set; }

    [Parameter]
    public RenderFragment DialogContent { get; set; }

    [Parameter]
    public RenderFragment DialogActions { get; set; }
}
