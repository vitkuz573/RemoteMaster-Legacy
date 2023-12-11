// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ConnectDialog : CommonDialogBase
{
    [Inject]
    private IComputerCommandService ComputerCommandService { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    protected string _selectedOption;

    protected async Task Connect()
    {
        if (_selectedOption == "control")
        {
            await ComputerCommandService.Execute(Hosts, async (computer, connection) => await OpenWindow($"/{computer.IPAddress}/access?imageQuality=25&cursorTracking=false&inputEnabled=true"));
        }
        else if (_selectedOption == "view")
        {
            await ComputerCommandService.Execute(Hosts, async (computer, connection) => await OpenWindow($"/{computer.IPAddress}/access?imageQuality=25&cursorTracking=true&inputEnabled=false"));
        }

        MudDialog.Close(DialogResult.Ok(true));
    }

    protected async Task OpenWindow(string url)
    {
        await JSRuntime.InvokeVoidAsync("openNewWindow", url);
    }
}
