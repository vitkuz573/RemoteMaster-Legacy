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
    private async Task OnMouseMove(MouseEventArgs e)
    {
        var (x, y) = await GetRelativeMousePositionPercentAsync(e);

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseCoordinates", new MouseMoveDto
        {
            X = x,
            Y = y
        }));
    }

    private async Task OnMouseUpDown(MouseEventArgs e)
    {
        var pressed = e.Type == "mousedown";

        await SendMouseInputAsync(e, pressed);
    }

    private async Task OnMouseOver(MouseEventArgs e)
    {
        await SendMouseInputAsync(e, false);
    }

    private async Task SendMouseInputAsync(MouseEventArgs e, bool pressed)
    {
        var (x, y) = await GetRelativeMousePositionPercentAsync(e);

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseButton", new MouseClickDto
        {
            Button = e.Button,
            Pressed = pressed,
            X = x,
            Y = y
        }));
    }

    private async Task OnMouseWheel(WheelEventArgs e)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseWheel", new MouseWheelDto
        {
            DeltaY = (int)e.DeltaY
        }));
    }

    private async Task SendKeyboardInput(int keyCode, bool pressed)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKeyboardInput", new KeyboardKeyDto
        {
            KeyCode = keyCode,
            Pressed = pressed
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
