// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Drawing;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.ScreenOverlays;

public class ClickIndicatorOverlay(int initialSize = 20, Color? indicatorColor = null, int animationDuration = 800) : IScreenOverlay
{
    private Point? _lastClickPosition;
    private readonly Stopwatch _stopwatch = new();
    private readonly Color _indicatorColor = indicatorColor ?? Color.Yellow;
    private readonly int _sizeIncrease = 20;

    public string Name => nameof(ClickIndicatorOverlay);

    public void RegisterClick(Point position)
    {
        _lastClickPosition = position;
        _stopwatch.Restart();
    }

    public void Draw(Graphics graphics, Rectangle screenBounds)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        if (!_lastClickPosition.HasValue || !_stopwatch.IsRunning)
        {
            return;
        }

        var elapsed = _stopwatch.ElapsedMilliseconds;
        var progress = Math.Min(1.0, elapsed / (double)animationDuration);

        if (progress >= 1.0)
        {
            _lastClickPosition = null;
            _stopwatch.Stop();

            return;
        }

        var size = (int)(initialSize + progress * _sizeIncrease);
        var alpha = (int)(128 * (1 - progress));

        using var brush = new SolidBrush(Color.FromArgb(alpha, _indicatorColor));
        var drawPosition = new Point(_lastClickPosition.Value.X - size / 2, _lastClickPosition.Value.Y - size / 2);

        graphics.FillEllipse(brush, drawPosition.X, drawPosition.Y, size, size);
    }

    public void Dispose()
    {
    }
}
