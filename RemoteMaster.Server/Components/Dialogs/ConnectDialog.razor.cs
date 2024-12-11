// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.JSInterop;
using MudBlazor;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class ConnectDialog : CommonDialogBase
{
    private string _selectedOption = "control";

    protected async Task Connect()
    {
        await HostCommandService.Execute(Hosts, async (host, _) => await OpenWindow($"/{host.IpAddress}/access?action={_selectedOption}"));

        MudDialog.Close(DialogResult.Ok(true));
    }

    private async Task OpenWindow(string url)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/windowOperations.js");

        await module.InvokeVoidAsync("openNewWindow", url, 600, 400);
    }
}
