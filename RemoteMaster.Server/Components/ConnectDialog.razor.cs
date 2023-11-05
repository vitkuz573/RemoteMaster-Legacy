// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components;

#pragma warning disable CA2227

public partial class ConnectDialog
{
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; }

    [Parameter]
    public Dictionary<Computer, HubConnection> Hosts { get; set; }

    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    private string _selectedOption;

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private async Task Connect()
    {
        if (_selectedOption == "control")
        {
            await ComputerCommandService.Execute(Hosts, async (computer, connection) => await OpenWindow($"/{computer.IPAddress}/connect?imageQuality=25&cursorTracking=false&inputEnabled=true"));
        }
        else if (_selectedOption == "view")
        {
            await ComputerCommandService.Execute(Hosts, async (computer, connection) => await OpenWindow($"/{computer.IPAddress}/connect?imageQuality=25&cursorTracking=true&inputEnabled=false"));
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task OpenWindow(string url)
    {
        await JSRuntime.InvokeVoidAsync("openNewWindow", url);
    }
}
