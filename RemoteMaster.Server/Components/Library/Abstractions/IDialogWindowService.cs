// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library.Abstractions;

public interface IDialogWindowService
{
    event Action<IDialogReference> OnShowDialog;

    Task<IDialogReference> ShowDialogAsync<TDialog>(string title) where TDialog : ComponentBase;

    Task<(bool, IDialogReference)> ShowConfirmationDialogAsync(string title, string message, string confirmText = "OK", string cancelText = "Cancel");
}