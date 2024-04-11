// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Components.Pages;

public partial class Access
{
    private async Task OnMouseEvent(MouseEventArgs e)
    {
        var (x, y) = await GetRelativeMousePositionPercentAsync(e);

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseInput", new MouseInputDto
        {
            Button = e.Button,
            IsPressed = e.Type == "mousedown",
            X = x,
            Y = y
        }));
    }

    private async Task OnMouseWheel(WheelEventArgs e)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseInput", new MouseInputDto
        {
            DeltaY = e.DeltaY
        }));
    }

    private async Task SendKeyboardInput(int keyCode, bool pressed)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKeyboardInput", new KeyboardInputDto
        {
            KeyCode = keyCode,
            IsPressed = pressed
        }));
    }

    [JSInvokable]
    public async Task OnKeyDown(int keyCode)
    {
        await SendKeyboardInput(keyCode, true);
    }

    [JSInvokable]
    public async Task OnKeyUp(int keyCode)
    {
        await SendKeyboardInput(keyCode, false);
    }
}
