// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Abstractions;

namespace RemoteMaster.Server.Components.Library;

public partial class DialogProvider
{
    private RenderFragment? _currentDialog;

    [Inject]
    private IDialogWindowService DialogService { get; set; }

    protected override void OnInitialized()
    {
        DialogService.OnShowDialog += SetDialog;
    }

    public void SetDialog(RenderFragment? dialog)
    {
        _currentDialog = dialog;
        InvokeAsync(StateHasChanged);
    }

    private DialogInstance CreateDialogInstance()
    {
        return new DialogInstance(() => SetDialog(null));
    }
}