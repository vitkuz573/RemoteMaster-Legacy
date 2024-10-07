// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Admin.Dialogs;

public partial class ConfirmationDialog
{
    [Parameter]
    public string Title { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = default!;

#pragma warning disable CA2227
    [Parameter]
    public Dictionary<string, string> Parameters { get; set; } = default!;
#pragma warning restore CA2227

    [Parameter]
    public EventCallback<bool> OnConfirm { get; set; }

    private bool _isVisible;

    public void Show(Dictionary<string, string> parameters)
    {
        Parameters = parameters;
        _isVisible = true;

        StateHasChanged();
    }

    private void Confirm(bool confirmed)
    {
        _isVisible = false;
        OnConfirm.InvokeAsync(confirmed);
    }
}
