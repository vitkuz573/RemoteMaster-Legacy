// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library.Models;

public class DialogReference(Guid dialogId, IDialogWindowService dialogService) : IDialogReference
{
    private readonly TaskCompletionSource<DialogResult> _resultCompletion = new();

    public Guid Id { get; private set; } = dialogId;

    public object Dialog { get; private set; }

    public RenderFragment RenderFragment { get; private set; }

    public Task<DialogResult> Result => _resultCompletion.Task;

    public void Close()
    {
        dialogService.Close(this);
    }

    public void Close(DialogResult result)
    {
        dialogService.Close(this, result);
    }

    public virtual bool Dismiss(DialogResult result)
    {
        return _resultCompletion.TrySetResult(result);
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
