// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Components.Library.Abstractions;

public interface IDialogWindowService
{
    event Action<string, string> OnShowDialog;

    event Func<string, string, string, string, Task<bool>> OnShowConfirmationDialog;

    Task ShowDialogAsync(string title, string message);

    Task<bool> ShowConfirmationDialogAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel");
}