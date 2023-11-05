// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

#pragma warning disable CA2227

public partial class DomainManagementDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }

    [Parameter]
    public Dictionary<Computer, HubConnection> Hosts { get; set; }

    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    private string _domain;
    private string _username;
    private string _password;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task JoinDomain()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendJoinToDomain", _domain, _username, _password));

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task LeaveDomain()
    {
        await ComputerCommandService.Execute(Hosts, async (computer, connection) => await connection.InvokeAsync("SendUnjoinFromDomain", _username, _password));

        MudDialog.Close(DialogResult.Ok(true));
    }
}
