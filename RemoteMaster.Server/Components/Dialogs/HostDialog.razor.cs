// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using MudBlazor;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class HostDialog
{
    [CascadingParameter]
    protected MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public HostDto HostDto { get; set; } = default!;

    private void Cancel()
    {
        MudDialog.Cancel();
    }
}
