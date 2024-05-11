// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library.Abstractions;

public interface IDialogWindowService
{
    event Action<IDialogReference> OnDialogInstanceAdded;
    event Action<IDialogReference> OnDialogCloseRequested;

    Task<IDialogReference> ShowAsync<T>(string title) where T : IComponent;

    void Close(IDialogReference dialog);

    IDialogReference CreateReference();
}