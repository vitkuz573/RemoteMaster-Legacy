// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogProvider
{
    private RenderFragment? CurrentDialog { get; set; }

    [Inject]
    private IDialogWindowService DialogService { get; set; }

    protected override void OnInitialized()
    {
        DialogService.OnShowDialog += SetDialog;
        DialogService.OnShowConfirmationDialog += SetConfirmationDialog;
    }

    public void SetDialog(RenderFragment? dialog)
    {
        CurrentDialog = dialog;
        InvokeAsync(StateHasChanged);
    }

    private async Task SetConfirmationDialog(string title, string message, string confirmText, string cancelText, TaskCompletionSource<bool> confirmationResult)
    {
        var dialogFragment = new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(ConfirmationDialog));
            builder.AddAttribute(1, "Title", title);
            builder.AddAttribute(2, "Message", message);
            builder.AddAttribute(3, "ConfirmText", confirmText);
            builder.AddAttribute(4, "CancelText", cancelText);
            builder.AddAttribute(5, "ConfirmationResult", confirmationResult);
            builder.AddAttribute(6, "CloseDialog", () => SetDialog(null));
            builder.CloseComponent();
        });

        SetDialog(dialogFragment);

        await InvokeAsync(StateHasChanged);
    }
}
