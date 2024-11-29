// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.ScreenOverlays;

public class CursorOverlay : IScreenOverlay
{
    private Point? _lastCursorPoint;
    private Icon? _lastCursorIcon;
    private readonly uint _cursorInfoSize = (uint)Marshal.SizeOf<CURSORINFO>();

    public string Name => nameof(CursorOverlay);

    public void Draw(Graphics graphics, Rectangle screenBounds)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        var cursorInfo = GetCursorInformation();

        if (cursorInfo.flags != CURSORINFO_FLAGS.CURSOR_SHOWING)
        {
            return;
        }

        var relativeX = cursorInfo.ptScreenPos.X - screenBounds.Left;
        var relativeY = cursorInfo.ptScreenPos.Y - screenBounds.Top;

        if (relativeX < 0 || relativeX >= screenBounds.Width || relativeY < 0 || relativeY >= screenBounds.Height)
        {
            return;
        }

        Icon icon;

        if (_lastCursorIcon == null || !_lastCursorPoint.HasValue || _lastCursorPoint.Value != cursorInfo.ptScreenPos)
        {
            icon = Icon.FromHandle(cursorInfo.hCursor);

            _lastCursorIcon?.Dispose();
            _lastCursorIcon = icon;
            _lastCursorPoint = cursorInfo.ptScreenPos;
        }
        else
        {
            icon = _lastCursorIcon;
        }

        graphics.DrawIcon(icon, relativeX, relativeY);
    }

    private CURSORINFO GetCursorInformation()
    {
        var cursorInfo = new CURSORINFO
        {
            cbSize = _cursorInfoSize
        };

        GetCursorInfo(ref cursorInfo);

        return cursorInfo;
    }

    public void Dispose()
    {
        _lastCursorIcon?.Dispose();
    }
}
