// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogProvider
{
    private Dictionary<Guid, (RenderFragment, DialogInstance)> _dialogs = [];

    protected override void OnInitialized()
    {
        DialogService.OnShowDialog += ShowDialog;
    }

    private void ShowDialog(Guid dialogId, RenderFragment dialog)
    {
        Log.Information("Adding dialog with ID: {DialogId}", dialogId);

        var dialogInstance = new DialogInstance();
        dialogInstance.OnClose += () => RemoveDialog(dialogId);

        _dialogs[dialogId] = (dialog, dialogInstance);

        InvokeAsync(StateHasChanged);
    }

    private void RemoveDialog(Guid dialogId)
    {
        Log.Information("Removing dialog with ID: {DialogId}", dialogId);

        _dialogs.Remove(dialogId);

        InvokeAsync(StateHasChanged);
    }
}