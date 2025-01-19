// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class DisconnectViewerDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public HubConnection HubConnection { get; set; } = default!;

    [Parameter]
    public ViewerDto Viewer { get; set; } = default!;   

    private DisconnectReason _selectedDisconnectReason = DisconnectReason.AdminInitiated;

    public void Cancel()
    {
        MudDialog.Close();
    }

    public async Task Disconnect()
    {
        var disconnectRequest = new ViewerDisconnectRequest(Viewer.ConnectionId, _selectedDisconnectReason);

        await HubConnection.InvokeAsync("DisconnectClient", disconnectRequest);

        MudDialog.Close();
    }
}
