// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class SslWarningDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string ContentText { get; set; } = default!;

    private void Discard()
    {
        MudDialog.Cancel();
    }

    private void Continue()
    {
        MudDialog.Close(DialogResult.Ok(string.Empty));
    }
}
