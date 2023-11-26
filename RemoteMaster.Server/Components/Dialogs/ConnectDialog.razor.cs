// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Components.Dialogs;

#pragma warning disable CA2227

public partial class ConnectDialog
{
    [CascadingParameter]
    public FluentDialog Dialog { get; set; } = default!;

    [Parameter]
    public Dictionary<Computer, HubConnection> Content { get; set; } = default!;

    [Inject]
    public IComputerCommandService ComputerCommandService { get; set; } = default!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    protected string _selectedOption;

    private async Task Connect()
    {
        if (_selectedOption == "control")
        {
            await ComputerCommandService.Execute(Content, async (computer, connection) => await OpenWindow($"/{computer.IPAddress}/connect?imageQuality=25&cursorTracking=false&inputEnabled=true"));
        }
        else if (_selectedOption == "view")
        {
            await ComputerCommandService.Execute(Content, async (computer, connection) => await OpenWindow($"/{computer.IPAddress}/connect?imageQuality=25&cursorTracking=true&inputEnabled=false"));
        }

        await Dialog.CloseAsync();
    }

    protected async Task OpenWindow(string url)
    {
        await JSRuntime.InvokeVoidAsync("openNewWindow", url);
    }
}
