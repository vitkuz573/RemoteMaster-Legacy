// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using MudBlazor;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class AddOuDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }

    [Parameter]
    public EventCallback<bool> OnOuAdded { get; set; }

    private readonly OrganizationalUnit _model = new()
    {
        Name = string.Empty
    };

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private async Task OnValidSubmit()
    {
        var newOu = new OrganizationalUnit
        {
            Name = _model.Name,
        };

        await DatabaseService.AddNodeAsync(newOu);

        await OnOuAdded.InvokeAsync(true);

        MudDialog.Close(DialogResult.Ok(true));
    }
}
