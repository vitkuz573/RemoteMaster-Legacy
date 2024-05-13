// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RemoteMaster.Server.Components.Library.Utilities;
using Serilog;

namespace RemoteMaster.Server.Components.Library;

public partial class SelectableArea
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public EventCallback<SelectionChangeEventArgs> OnSelectionChanged { get; set; }

    [Parameter, EditorRequired]
    public string ContainerId { get; set; } = null!;

    public List<string> SelectedElementIds { get; private set; } = [];

    protected string ContainerClasses => new CssClassBuilder()
        .AddBase("relative user-select-none w-full h-full border")
        .Add("border-gray-300")
        .Build();

    private bool _isSelecting;
    private Point _startPoint;
    private string _selectionBoxStyle = string.Empty;
    private Rectangle _selectionRectangle;
    private Rectangle _containerRectangle;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/SelectableArea.js");
            await module.InvokeVoidAsync("trackSelectedElements", ContainerId, ".selectable", new string[] { "ring-2", "ring-blue-500", "shadow-lg" } , DotNetObjectReference.Create(this));
        }
    }

    protected async Task OnMouseDown(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var area = await JsRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", ContainerId);
        _containerRectangle = await area.InvokeAsync<Rectangle>("getBoundingClientRect");

        _startPoint = new Point((int)(e.ClientX - _containerRectangle.X), (int)(e.ClientY - _containerRectangle.Y));
        _selectionRectangle = new Rectangle(_startPoint.X, _startPoint.Y, 0, 0);
        _isSelecting = true;

        UpdateSelectionBox();
    }

    protected void OnMouseMove(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_isSelecting)
        {
            var adjustedX = (int)(e.ClientX - _containerRectangle.X);
            var adjustedY = (int)(e.ClientY - _containerRectangle.Y);

            UpdateSelectionRectangle(adjustedX, adjustedY);
            UpdateSelectionBox();
        }
    }

    protected void OnMouseUp(MouseEventArgs e)
    {
        _isSelecting = false;

        ClearSelectionBox();
    }

    private void ClearSelectionBox()
    {
        _selectionBoxStyle = "";
    }

    private void UpdateSelectionRectangle(int adjustedX, int adjustedY)
    {
        var x = Math.Min(_startPoint.X, adjustedX);
        var y = Math.Min(_startPoint.Y, adjustedY);
        var width = Math.Abs(_startPoint.X - adjustedX);
        var height = Math.Abs(_startPoint.Y - adjustedY);

        _selectionRectangle = new Rectangle(x, y, width, height);
    }

    private void UpdateSelectionBox()
    {
        var styles = new CssStyleBuilder()
            .Add("left", $"{_selectionRectangle.X}px")
            .Add("top", $"{_selectionRectangle.Y}px")
            .Add("width", $"{_selectionRectangle.Width}px")
            .Add("height", $"{_selectionRectangle.Height}px")
            .Build();

        _selectionBoxStyle = styles;
    }

    private static string GetSelectionBoxClasses()
    {
        var builder = new CssClassBuilder()
            .AddBase("absolute bg-blue-700 opacity-30 border border-blue-900");

        return builder.Build();
    }

    private void NotifySelectionChanged()
    {
        if (OnSelectionChanged.HasDelegate)
        {
            var args = new SelectionChangeEventArgs
            {
                SelectionRectangle = _selectionRectangle,
                SelectedElementIds = SelectedElementIds
            };

            OnSelectionChanged.InvokeAsync(args);
        }
    }

    [JSInvokable]
    public void UpdateSelectedElements(List<string> selectedElementIds)
    {
        SelectedElementIds = selectedElementIds;

        NotifySelectionChanged();
    }
}