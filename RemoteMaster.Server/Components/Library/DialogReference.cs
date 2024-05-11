// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library;

public class DialogReference(Guid dialogId, IDialogWindowService dialogService) : IDialogReference
{
    public Guid Id { get; private set; } = dialogId;

    public object Dialog { get; private set; }

    public RenderFragment RenderFragment { get; private set; }

    public void Close()
    {
        dialogService.Close(this);
    }

    public void InjectDialog(object inst)
    {
        Dialog = inst;
    }

    public void InjectRenderFragment(RenderFragment rf)
    {
        RenderFragment = rf;
    }
}
