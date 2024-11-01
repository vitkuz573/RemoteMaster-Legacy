// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.ScreenOverlays;

public class ClickIndicatorOverlay(ILogger<ClickIndicatorOverlay> logger) : IScreenOverlay
{
    private Point? _lastClickPosition;

    public void RegisterClick(Point position)
    {
        _lastClickPosition = position;
        logger.LogInformation("Registered click at position: {Position}", position);
    }

    public void Draw(Graphics graphics, Rectangle screenBounds)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        if (!_lastClickPosition.HasValue)
        {
            logger.LogDebug("No click position to draw.");
            return;
        }

        using var brush = new SolidBrush(Color.Red);
        var drawPosition = new Point(_lastClickPosition.Value.X - 10, _lastClickPosition.Value.Y - 10);

        logger.LogInformation("Drawing click indicator at position: {DrawPosition}", drawPosition);

        graphics.FillEllipse(brush, drawPosition.X, drawPosition.Y, 20, 20);

        _lastClickPosition = null;
        logger.LogDebug("Reset last click position after drawing.");
    }
}
