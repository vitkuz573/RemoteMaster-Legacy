// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.ScreenOverlays;

public class ClickIndicatorOverlay : IScreenOverlay
{
    private Point? _lastClickPosition;

    public void RegisterClick(Point position)
    {
        _lastClickPosition = position;
    }

    public void Draw(Graphics graphics, Rectangle screenBounds)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        if (!_lastClickPosition.HasValue)
        {
            return;
        }

        using var brush = new SolidBrush(Color.FromArgb(128, Color.Yellow));
        var drawPosition = new Point(_lastClickPosition.Value.X - 10, _lastClickPosition.Value.Y - 10);

        graphics.FillEllipse(brush, drawPosition.X, drawPosition.Y, 20, 20);

        _lastClickPosition = null;
    }
}
