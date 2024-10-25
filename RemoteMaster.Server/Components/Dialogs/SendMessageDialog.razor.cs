// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class SendMessageDialog
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private string? _userName;
    private string _message = string.Empty;

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;

        _userName = user.FindFirstValue(ClaimTypes.Name);
    }

    protected async Task Send()
    {
        try
        {
            if (_userName == null)
            {
                throw new InvalidOperationException("Username claim not found.");
            }

            var chatMessageDto = new ChatMessageDto(_userName, _message);

            await HostCommandService.Execute(Hosts, async (_, connection) => await connection!.InvokeAsync("SendMessage", chatMessageDto));
        }
        catch (Exception)
        {
            // ignored
        }

        MudDialog.Close(DialogResult.Ok(true));
    }
}
