// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library;

public partial class ConfirmationDialog
{
    [Parameter]
    public string Title { get; set; }

    [Parameter]
    public string Message { get; set; }

    [Parameter]
    public string ConfirmText { get; set; } = "OK";

    [Parameter]
    public string CancelText { get; set; } = "Cancel";

    [Parameter]
    public TaskCompletionSource<bool> ConfirmationResult { get; set; }

    [Parameter]
    public Action CloseDialog { get; set; }

    private void Confirm()
    {
        ConfirmationResult.SetResult(true);
        CloseDialog?.Invoke();
    }

    private void Cancel()
    {
        ConfirmationResult.SetResult(false);
        CloseDialog?.Invoke();
    }
}
