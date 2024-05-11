// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Components.Library.Services;

public class DialogService : IDialogWindowService
{
    public event Action<IDialogReference> OnShowDialog;

    public Task<IDialogReference> ShowDialogAsync<TDialog>(string title) where TDialog : ComponentBase
    {
        var dialogId = Guid.NewGuid();
        var dialogInstance = new DialogInstance();
        var dialogFragment = new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(TDialog));
            builder.AddAttribute(1, "Title", title);
            builder.CloseComponent();
        });

        IDialogReference dialogReference = new DialogReference(dialogId, dialogFragment, dialogInstance);

        Log.Information("Creating dialog of type {DialogType} with title '{Title}' and ID {DialogId}", typeof(TDialog).Name, title, dialogId);
        OnShowDialog?.Invoke(dialogReference);

        return Task.FromResult(dialogReference);
    }


    public Task<(bool, IDialogReference)> ShowConfirmationDialogAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        var dialogId = Guid.NewGuid();
        var dialogInstance = new DialogInstance();
        var confirmationResult = new TaskCompletionSource<bool>();

        var dialogFragment = new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(ConfirmationDialog));
            builder.AddAttribute(1, "Title", title);
            builder.AddAttribute(2, "Message", message);
            builder.AddAttribute(3, "ConfirmText", confirmText);
            builder.AddAttribute(4, "CancelText", cancelText);
            builder.AddAttribute(5, "ConfirmationResult", confirmationResult);
            builder.CloseComponent();
        });

        IDialogReference dialogReference = new DialogReference(dialogId, dialogFragment, dialogInstance);

        Log.Information("Creating confirmation dialog with title '{Title}' and ID {DialogId}", title, dialogId);
        OnShowDialog?.Invoke(dialogReference);

        return Task.FromResult((confirmationResult.Task.Result, dialogReference));
    }
}