// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace RemoteMaster.Server.Components.Pages;

public partial class Access
{
    private RenderFragment RenderScreenImage()
    {
        return builder =>
        {
            if (_isAccessDenied)
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, "Access Denied. Please contact the administrator");
                builder.CloseElement();
            }
            else if (_connectionFailed)
            {
                builder.OpenElement(2, "p");
                builder.AddContent(3, "Unable to establish connection. Please try again later.");
                builder.CloseElement();
            }
            else if (string.IsNullOrEmpty(_screenDataUrl))
            {
                builder.OpenElement(4, "p");
                builder.AddContent(5, "Establishing connection...");
                builder.CloseElement();
            }
            else
            {
                builder.OpenElement(6, "img");

                var cssClass = _selectedDisplay == "VIRTUAL_SCREEN"
                    ? "h-auto max-w-full object-contain mx-auto"
                    : "max-h-full w-auto object-contain mx-auto";

                builder.AddEventPreventDefaultAttribute(7, "oncontextmenu", true);

                var attributes = new Dictionary<string, object>
                {
                    { "src", _screenDataUrl },
                    { "class", cssClass },
                    { "oncontextmenu", EventCallback.Factory.Create<MouseEventArgs>(this, () => Task.CompletedTask) },
                    { "draggable", "false" },
                    { "onload", EventCallback.Factory.Create<EventArgs>(this, OnLoadAsync) },
                    { "onmousemove", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseEvent) },
                    { "onmousedown", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseEvent) },
                    { "onmouseup", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseEvent) },
                    { "onmouseover", EventCallback.Factory.Create<MouseEventArgs>(this, OnMouseEvent) },
                    { "onmousewheel", EventCallback.Factory.Create<WheelEventArgs>(this, OnMouseWheel) },
                    { "alt", string.Empty }
                };

                builder.AddMultipleAttributes(8, attributes);

                builder.AddElementReferenceCapture(9, element => _screenImageElement = element);

                builder.CloseElement();
            }
        };
    }
}
