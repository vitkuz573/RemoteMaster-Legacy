// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Components.Library.Enums;

namespace RemoteMaster.Server.Components.Library;

public partial class Button : IHandleEvent
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public bool IsDisabled
    {
        get => _isDisabled;
        set
        {
            if (_isDisabled != value)
            {
                _isDisabled = value;

                IsDisabledChanged.InvokeAsync(value);
            }
        }
    }

    [Parameter]
    public EventCallback<bool> IsDisabledChanged { get; set; }

    [Parameter]
    public string Icon { get; set; } = string.Empty;

    [Parameter]
    public ButtonType ButtonType { get; set; } = ButtonType.Button;

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public EventCallback OnMouseOver { get; set; }

    [Parameter]
    public EventCallback OnMouseOut { get; set; }

    [Parameter]
    public bool IsToggled
    {
        get => _isToggled;
        set
        {
            if (_isToggled != value)
            {
                _isToggled = value;

                IsToggledChanged.InvokeAsync(value);
            }
        }
    }

    [Parameter]
    public EventCallback<bool> IsToggledChanged { get; set; }

    [Parameter]
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;

                IsLoadingChanged.InvokeAsync(value);
            }
        }
    }

    [Parameter]
    public EventCallback<bool> IsLoadingChanged { get; set; }

    [Parameter]
    public ButtonSize Size { get; set; } = ButtonSize.Medium;

    [Parameter]
    public ButtonColor Color { get; set; } = ButtonColor.Primary;

    private bool _isDisabled;
    private bool _isToggled;
    private bool _isLoading;

    private async Task HandleClick()
    {
        if (!IsDisabled)
        {
            await OnClick.InvokeAsync();
        }
    }

    private async Task HandleMouseOver()
    {
        await OnMouseOver.InvokeAsync();
    }

    private async Task HandleMouseOut()
    {
        await OnMouseOut.InvokeAsync();
    }

    private string GetButtonClasses()
    {
        var colorClasses = Color switch
        {
            ButtonColor.Primary => "bg-blue-600 hover:bg-blue-700",
            ButtonColor.Secondary => "bg-gray-600 hover:bg-gray-700",
            ButtonColor.Success => "bg-green-600 hover:bg-green-700",
            ButtonColor.Danger => "bg-red-600 hover:bg-red-700",
            _ => "bg-blue-600 hover:bg-blue-700"
        };

        var sizeClasses = Size switch
        {
            ButtonSize.Small => "px-2 py-1 text-sm",
            ButtonSize.Large => "px-6 py-3 text-lg",
            _ => "px-4 py-2 text-base"
        };

        var classes = $"rounded-lg px-4 py-2 font-semibold text-white hover:bg-blue-700 {colorClasses} {sizeClasses}";

        if (IsDisabled)
        {
            classes += " bg-gray-400 hover:bg-gray-400 cursor-not-allowed opacity-50";
        }

        return classes;
    }

    private string GetButtonTypeAsString() => ButtonType.ToString().ToLower();
}
