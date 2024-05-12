// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Serilog;

namespace RemoteMaster.Server.Components.Library;

public partial class SelectableArea
{
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    [Parameter]
    public EventCallback<Rectangle> OnSelectionChanged { get; set; }

    protected string ContainerStyle => "position: relative; user-select: none; width: 100%; height: 100%; min-height: 100px; min-width: 100px; border: 1px solid #ccc;";

    protected string SelectionBoxStyle { get; set; }

    private bool _isSelecting;
    private Point _startPoint;
    private Rectangle _selectionRectangle;

    protected async Task OnMouseDown(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var area = await JsRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "selectable-container");
        var rect = await area.InvokeAsync<Rectangle>("getBoundingClientRect");
        _startPoint = new Point((int)(e.ClientX - rect.X), (int)(e.ClientY - rect.Y));
        _selectionRectangle = new Rectangle(_startPoint.X, _startPoint.Y, 0, 0);
        _isSelecting = true;

        Log.Information("MouseDown at X: {X}, Y: {Y}, Rect: {Rect}", _startPoint.X, _startPoint.Y, _selectionRectangle);

        UpdateSelectionBox();
    }

    protected async Task OnMouseMove(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_isSelecting)
        {
            var area = await JsRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "selectable-container");
            var rect = await area.InvokeAsync<Rectangle>("getBoundingClientRect");

            int adjustedX = (int)(e.ClientX - rect.X);
            int adjustedY = (int)(e.ClientY - rect.Y);

            UpdateSelectionRectangle(adjustedX, adjustedY);
            UpdateSelectionBox();

            Log.Information("Raw MouseMove ClientX: {ClientX}, ClientY: {ClientY}, AdjustedX: {AdjustedX}, AdjustedY: {AdjustedY}, Rect: {Rect}",
                            e.ClientX, e.ClientY, adjustedX, adjustedY, _selectionRectangle);
        }
    }

    protected void OnMouseUp(MouseEventArgs e)
    {
        _isSelecting = false;
        ClearSelectionBox();
        NotifySelectionChanged();
        Log.Information("MouseUp with final rectangle {Rectangle}", _selectionRectangle);
    }

    private void ClearSelectionBox()
    {
        SelectionBoxStyle = "";
    }

    private void UpdateSelectionRectangle(int adjustedX, int adjustedY)
    {
        var x = Math.Min(_startPoint.X, adjustedX);
        var y = Math.Min(_startPoint.Y, adjustedY);
        var width = Math.Abs(_startPoint.X - adjustedX);
        var height = Math.Abs(_startPoint.Y - adjustedY);

        _selectionRectangle = new Rectangle(x, y, width, height);

        Log.Information("UpdateSelectionRectangle to X: {X}, Y: {Y}, Width: {Width}, Height: {Height}", x, y, width, height);
    }

    private void UpdateSelectionBox()
    {
        SelectionBoxStyle = $"position: absolute; left: {_selectionRectangle.X}px; top: {_selectionRectangle.Y}px; " +
                            $"width: {_selectionRectangle.Width}px; height: {_selectionRectangle.Height}px; " +
                            $"background-color: rgba(0, 120, 215, 0.3); border: 1px solid #0078D7;";
    }

    private void NotifySelectionChanged()
    {
        if (OnSelectionChanged.HasDelegate)
        {
            OnSelectionChanged.InvokeAsync(_selectionRectangle);
        }
    }
}