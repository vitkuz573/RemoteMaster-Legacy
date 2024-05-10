// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library.Services;

public class DialogService : IDialogWindowService
{
    public event Action<RenderFragment> OnShowDialog;

    public Task ShowDialogAsync<TDialog>(string title) where TDialog : ComponentBase
    {
        var dialogFragment = new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(TDialog));
            builder.AddAttribute(1, "Title", title);
            builder.CloseComponent();
        });

        OnShowDialog?.Invoke(dialogFragment);

        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationDialogAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        var confirmationResult = new TaskCompletionSource<bool>();
        
        OnShowDialog?.Invoke(new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(ConfirmationDialog));
            builder.AddAttribute(1, "Title", title);
            builder.AddAttribute(2, "Message", message);
            builder.AddAttribute(3, "ConfirmText", confirmText);
            builder.AddAttribute(4, "CancelText", cancelText);
            builder.AddAttribute(5, "ConfirmationResult", confirmationResult);
            builder.CloseComponent();
        }));

        return confirmationResult.Task;
    }
}