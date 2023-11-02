// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Pages;

public partial class Connect
{
    private async Task OnMouseMove(MouseEventArgs e)
    {
        var xyPercent = await GetRelativeMousePositionPercentAsync(e);

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseCoordinates", new MouseMoveDto
        {
            X = xyPercent.X,
            Y = xyPercent.Y
        }));
    }

    private async Task OnMouseUpDown(MouseEventArgs e)
    {
        var state = e.Type == "mouseup" ? ButtonState.Up : ButtonState.Down;
        await SendMouseInputAsync(e, state);
    }

    private async Task OnMouseOver(MouseEventArgs e)
    {
        await SendMouseInputAsync(e, ButtonState.Up);
    }

    private async Task SendMouseInputAsync(MouseEventArgs e, ButtonState state)
    {
        var xyPercent = await GetRelativeMousePositionPercentAsync(e);

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseButton", new MouseClickDto
        {
            Button = e.Button,
            State = state,
            X = xyPercent.X,
            Y = xyPercent.Y
        }));
    }

    private async Task OnMouseWheel(WheelEventArgs e)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseWheel", new MouseWheelDto
        {
            DeltaY = (int)e.DeltaY
        }));
    }

    private async Task SendKeyboardInput(int keyCode, ButtonState state)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKeyboardInput", new KeyboardKeyDto
        {
            Key = keyCode,
            State = state
        }));
    }

    [JSInvokable]
    public async Task OnKeyDown(int keyCode)
    {
        await SendKeyboardInput(keyCode, ButtonState.Down);
    }

    [JSInvokable]
    public async Task OnKeyUp(int keyCode)
    {
        await SendKeyboardInput(keyCode, ButtonState.Up);
    }
}
