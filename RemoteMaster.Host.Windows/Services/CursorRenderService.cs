// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.InteropServices;
using RemoteMaster.Host.Windows.Abstractions;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CursorRenderService : ICursorRenderService
{
    private Point? _lastCursorPoint;
    private Icon? _lastCursorIcon;
    private Rectangle _cachedScreenBounds;
    private readonly uint _cursorInfoSize = (uint)Marshal.SizeOf<CURSORINFO>();

    public void DrawCursor(Graphics g, Rectangle currentScreenBounds)
    {
        ArgumentNullException.ThrowIfNull(g);

        _cachedScreenBounds = currentScreenBounds;

        var cursorInfo = GetCursorInformation();

        if (cursorInfo.flags != CURSORINFO_FLAGS.CURSOR_SHOWING)
        {
            return;
        }

        var relativeX = cursorInfo.ptScreenPos.X - _cachedScreenBounds.Left;
        var relativeY = cursorInfo.ptScreenPos.Y - _cachedScreenBounds.Top;

        if (relativeX < 0 || relativeX >= _cachedScreenBounds.Width || relativeY < 0 || relativeY >= _cachedScreenBounds.Height)
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

        g.DrawIcon(icon, relativeX, relativeY);
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

    public void ClearCache()
    {
        _lastCursorIcon?.Dispose();
        _lastCursorPoint = null;
        _cachedScreenBounds = Rectangle.Empty;
    }

    public void Dispose()
    {
        _lastCursorIcon?.Dispose();
    }
}
