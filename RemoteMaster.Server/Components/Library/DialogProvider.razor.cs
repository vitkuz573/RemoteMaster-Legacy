// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Components.Library.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogProvider
{
    private readonly List<IDialogReference> _dialogs = [];

    protected override void OnInitialized()
    {
        DialogService.OnShowDialog += AddDialog;
    }

    private void AddDialog(IDialogReference dialogReference)
    {
        Log.Information("Adding dialog with ID: {DialogId}", dialogReference.DialogId);
        
        dialogReference.Instance.OnClose += () => RemoveDialog(dialogReference);

        _dialogs.Add(dialogReference);

        InvokeAsync(StateHasChanged);
    }

    private void RemoveDialog(IDialogReference dialogReference)
    {
        if (dialogReference != null)
        {
            Log.Information("Removing dialog with ID: {DialogId}", dialogReference.DialogId);
            
            _dialogs.Remove(dialogReference);
            
            InvokeAsync(StateHasChanged);
        }
    }
}