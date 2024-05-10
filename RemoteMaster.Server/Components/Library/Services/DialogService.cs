// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library.Services;

public class DialogService : IDialogWindowService
{
    public event Action<RenderFragment> OnShowDialog;
    public event Func<string, string, string, string, TaskCompletionSource<bool>, Task> OnShowConfirmationDialog;

    public Task ShowDialogAsync<TDialog>() where TDialog : ComponentBase
    {
        var dialogFragment = new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(TDialog));
            builder.CloseComponent();
        });

        OnShowDialog?.Invoke(dialogFragment);

        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationDialogAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        var confirmationResult = new TaskCompletionSource<bool>();
        OnShowConfirmationDialog?.Invoke(title, message, confirmText, cancelText, confirmationResult);
        
        return confirmationResult.Task;
    }
}