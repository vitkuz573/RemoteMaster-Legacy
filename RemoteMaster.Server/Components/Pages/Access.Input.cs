// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Components.Pages;

public partial class Access
{
    private async Task<PointF> GetRelativeMousePositionPercentAsync(MouseEventArgs e)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blobUtils.js");

        var imgPosition = await module.InvokeAsync<RectangleF>("getElementRect", _screenImageElement);

        var percentX = (float)(e.ClientX - imgPosition.Left) / imgPosition.Width;
        var percentY = (float)(e.ClientY - imgPosition.Top) / imgPosition.Height;

        return new PointF(percentX, percentY);
    }

    private async Task OnMouseEvent(MouseEventArgs e)
    {
        var position = await GetRelativeMousePositionPercentAsync(e);

        var mouseInputDto = new MouseInputDto
        {
            Position = position
        };

        switch (e.Type)
        {
            case "mousedown":
                mouseInputDto.Button = e.Button;
                mouseInputDto.IsPressed = true;
                break;
            case "mouseup":
            case "mouseover":
                mouseInputDto.Button = e.Button;
                mouseInputDto.IsPressed = false;
                break;
            case "mousemove":
                break;
            default:
                return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseInput", mouseInputDto));
    }

    private async Task OnMouseWheel(WheelEventArgs e)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseInput", new MouseInputDto
        {
            DeltaY = e.DeltaY
        }));
    }

    private async Task SendKeyboardInput(string code, bool isPressed)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKeyboardInput", new KeyboardInputDto
        {
            Code = code,
            IsPressed = isPressed
        }));
    }

    [JSInvokable]
    public async Task OnKeyDown(string code)
    {
        await SendKeyboardInput(code, true);
    }

    [JSInvokable]
    public async Task OnKeyUp(string code)
    {
        await SendKeyboardInput(code, false);
    }
}
