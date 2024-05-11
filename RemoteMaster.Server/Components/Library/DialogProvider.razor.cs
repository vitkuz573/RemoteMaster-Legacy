// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogProvider
{
    private readonly List<IDialogReference> _dialogs = [];

    protected override void OnInitialized()
    {
        DialogService.OnShowDialog += ShowDialog;
    }

    private void ShowDialog(Guid dialogId, RenderFragment dialog)
    {
        Log.Information("Adding dialog with ID: {DialogId}", dialogId);

        var dialogInstance = new DialogInstance();
        dialogInstance.OnClose += () => RemoveDialog(dialogId);

        var dialogReference = new DialogReference(dialogId, dialog, dialogInstance);
        _dialogs.Add(dialogReference);

        InvokeAsync(StateHasChanged);
    }

    private void RemoveDialog(Guid dialogId)
    {
        Log.Information("Removing dialog with ID: {DialogId}", dialogId);
        
        var dialogToRemove = _dialogs.FirstOrDefault(d => d.DialogId == dialogId);
        
        if (dialogToRemove != null)
        {
            _dialogs.Remove(dialogToRemove);
        }

        InvokeAsync(StateHasChanged);
    }
}