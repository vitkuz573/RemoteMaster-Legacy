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
    public EventCallback<Rectangle> OnSelectionChanged { get; set; }

    [Parameter]
    public EventCallback<List<string>> SelectedElementsChanged { get; set; }

    [Parameter]
    public string ContainerId { get; set; } = "selectable-container";

    public List<string> SelectedElementIds { get; private set; } = [];

    protected string ContainerClasses => new CssClassBuilder()
        .AddBase("relative user-select-none w-full h-full border")
        .Add("border-gray-300")
        .Build();

    protected string SelectionBoxStyle { get; set; }

    private bool _isSelecting;
    private Point _startPoint;
    private Rectangle _selectionRectangle;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("trackSelectedElements", ContainerId, DotNetObjectReference.Create(this));
        }
    }

    protected async Task OnMouseDown(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var area = await JsRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", ContainerId);
        var rect = await area.InvokeAsync<Rectangle>("getBoundingClientRect");
        
        _startPoint = new Point((int)(e.ClientX - rect.X), (int)(e.ClientY - rect.Y));
        _selectionRectangle = new Rectangle(_startPoint.X, _startPoint.Y, 0, 0);
        _isSelecting = true;

        UpdateSelectionBox();
    }

    protected async Task OnMouseMove(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_isSelecting)
        {
            var area = await JsRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", ContainerId);
            var rect = await area.InvokeAsync<Rectangle>("getBoundingClientRect");

            var adjustedX = (int)(e.ClientX - rect.X);
            var adjustedY = (int)(e.ClientY - rect.Y);

            UpdateSelectionRectangle(adjustedX, adjustedY);
            UpdateSelectionBox();
        }
    }

    protected void OnMouseUp(MouseEventArgs e)
    {
        _isSelecting = false;

        ClearSelectionBox();
        NotifySelectionChanged();
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
    }

    private void UpdateSelectionBox()
    {
        var styles = new CssStyleBuilder()
            .Add("position", "absolute")
            .Add("left", $"{_selectionRectangle.X}px")
            .Add("top", $"{_selectionRectangle.Y}px")
            .Add("width", $"{_selectionRectangle.Width}px")
            .Add("height", $"{_selectionRectangle.Height}px")
            .Add("background-color", "rgba(0, 120, 215, 0.3)")
            .Add("border", "1px solid #0078D7")
            .Build();

        SelectionBoxStyle = styles;
    }

    private void NotifySelectionChanged()
    {
        if (OnSelectionChanged.HasDelegate)
        {
            OnSelectionChanged.InvokeAsync(_selectionRectangle);
        }
    }

    [JSInvokable]
    public async Task UpdateSelectedElements(List<string> selectedElementIds)
    {
        SelectedElementIds = selectedElementIds;

        Log.Information("Selected elements updated: {@SelectedElementIds}", SelectedElementIds);

        await SelectedElementsChanged.InvokeAsync(SelectedElementIds);
    }
}