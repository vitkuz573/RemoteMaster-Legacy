// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Components.Library.Services;

public class DialogService : IDialogWindowService
{
    public event Action<IDialogReference> OnDialogInstanceAdded;

    public Task<IDialogReference> ShowAsync<T>(string title) where T : IComponent
    {
        var dialogReference = CreateReference();

        var dialogFragment = new RenderFragment(builder =>
        {
            builder.OpenComponent(0, typeof(T));
            builder.AddAttribute(1, "Title", title);
            builder.CloseComponent();
        });

        dialogReference.InjectRenderFragment(dialogFragment);

        Log.Information("Creating dialog of type {DialogType} with title '{Title}' and ID {DialogId}", typeof(T).Name, title, dialogReference.Id);
        OnDialogInstanceAdded?.Invoke(dialogReference);

        return Task.FromResult(dialogReference);
    }

    public Task<(bool, IDialogReference)> ShowMessageBox(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        var dialogReference = CreateReference();
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

        dialogReference.InjectRenderFragment(dialogFragment);

        Log.Information("Creating confirmation dialog with title '{Title}' and ID {DialogId}", title, dialogReference.Id);
        
        OnDialogInstanceAdded?.Invoke(dialogReference);

        return confirmationResult.Task.ContinueWith(task => (task.Result, dialogReference));
    }

    public IDialogReference CreateReference()
    {
        return new DialogReference(Guid.NewGuid(), new DialogInstance());
    }
}