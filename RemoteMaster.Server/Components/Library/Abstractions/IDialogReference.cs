// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;

namespace RemoteMaster.Server.Components.Library.Abstractions;

public interface IDialogReference
{
    Guid Id { get; }

    object Dialog { get; }

    RenderFragment RenderFragment { get; }

    Task<DialogResult> Result { get; }

    void Close();

    void Close(DialogResult result);

    bool Dismiss(DialogResult result);

    void InjectDialog(object inst);

    void InjectRenderFragment(RenderFragment rf);
}

