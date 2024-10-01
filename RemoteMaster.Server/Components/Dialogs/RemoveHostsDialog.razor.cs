// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using MudBlazor;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class RemoveHostsDialog
{
    [Parameter]
    public EventCallback<IEnumerable<HostDto>> OnHostsRemoved { get; set; }

    private async Task Delete()
    {
        foreach (var (host, _) in Hosts)
        {
            await OrganizationService.RemoveHostAsync(host.OrganizationId, host.OrganizationalUnitId, host.Id);

            await OnHostsRemoved.InvokeAsync(Hosts.Keys);
        }

        await InvokeAsync(StateHasChanged);

        MudDialog.Close(DialogResult.Ok(true));
    }
}
