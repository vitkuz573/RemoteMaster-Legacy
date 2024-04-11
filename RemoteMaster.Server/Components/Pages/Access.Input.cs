// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Dtos;
using PointD = (double, double);

namespace RemoteMaster.Server.Components.Pages;

public partial class Access
{
    private async Task<PointD> GetRelativeMousePositionPercentAsync(MouseEventArgs e)
    {
        var imgElement = await JsRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<DomRect>("getBoundingClientRect");

        var percentX = (e.ClientX - imgPosition.Left) / imgPosition.Width;
        var percentY = (e.ClientY - imgPosition.Top) / imgPosition.Height;

        return new PointD(percentX, percentY);
    }

    private async Task OnMouseEvent(MouseEventArgs e)
    {
        var (x, y) = await GetRelativeMousePositionPercentAsync(e);

        var mouseInputDto = new MouseInputDto
        {
            X = x,
            Y = y
        };

        switch (e.Type)
        {
            case "mousedown":
            case "mouseup":
                mouseInputDto.Button = e.Button;
                mouseInputDto.IsPressed = e.Type == "mousedown";
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

    private async Task OnKeyEvent(KeyboardEventArgs e)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKeyboardInput", new KeyboardInputDto
        {
            KeyCode = Convert.ToInt32(e.Code),
            IsPressed = e.Type == "keydown"
        }));
    }
}
