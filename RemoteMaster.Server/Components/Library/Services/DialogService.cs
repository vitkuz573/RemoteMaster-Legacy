// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library.Services;

public class DialogService : IDialogWindowService
{
    public event Action<string, string> OnShowDialog;
    public event Func<string, string, string, string, Task<bool>> OnShowConfirmationDialog;

    public Task ShowDialogAsync(string title, string message)
    {
        OnShowDialog?.Invoke(title, message);

        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationDialogAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel")
    {
        var confirmation = OnShowConfirmationDialog?.Invoke(title, message, confirmText, cancelText);

        return confirmation ?? Task.FromResult(false);
    }
}