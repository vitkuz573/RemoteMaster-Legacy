// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Models;

namespace RemoteMaster.Server.Components.Library.Abstractions;

public interface IDialogWindowService
{
    event Action<IDialogReference> OnDialogInstanceAdded;

    event Action<IDialogReference, DialogResult> OnDialogCloseRequested;

    IDialogReference Show(Type contentComponent, string title, DialogParameters parameters, DialogOptions options);

    Task<IDialogReference> ShowAsync<T>(string title, DialogParameters parameters, DialogOptions options) where T : IComponent;

    Task<IDialogReference> ShowAsync(Type contentComponent, string title, DialogParameters parameters, DialogOptions options);

    IDialogReference CreateReference();

    void Close(IDialogReference dialog);

    void Close(IDialogReference dialog, DialogResult result);
}