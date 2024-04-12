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
        var imgElement = await JsRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<RectangleF>("getBoundingClientRect");

        var percentX = (float)(e.ClientX - imgPosition.Left) / imgPosition.Width;
        var percentY = (float)(e.ClientY - imgPosition.Top) / imgPosition.Height;

        return new PointF(percentX, percentY);
    }

    private async Task OnMouseEvent(EventArgs e)
    {
        MouseInputDto? mouseInputDto = null;

        if (e is MouseEventArgs mouseEventArgs)
        {
            var position = await GetRelativeMousePositionPercentAsync(mouseEventArgs);

            mouseInputDto = new MouseInputDto
            {
                Position = position
            };

            switch (mouseEventArgs.Type)
            {
                case "mousedown":
                case "mouseup":
                    mouseInputDto.Button = mouseEventArgs.Button;
                    mouseInputDto.IsPressed = mouseEventArgs.Type == "mousedown";
                    break;
                case "mousemove":
                    break;
                default:
                    return;
            }
        }
        else if (e is WheelEventArgs wheelEventArgs)
        {
            mouseInputDto = new MouseInputDto
            {
                DeltaY = wheelEventArgs.DeltaY
            };
        }
        else
        {
            return;
        }

        if (mouseInputDto != null)
        {
            await SafeInvokeAsync(() => _connection.InvokeAsync("SendMouseInput", mouseInputDto));
        }
    }

    private async Task OnKeyboardEvent(KeyboardEventArgs e)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKeyboardInput", new KeyboardInputDto
        {
            KeyCode = Convert.ToInt32(e.Code),
            IsPressed = e.Type == "keydown"
        }));
    }
}
