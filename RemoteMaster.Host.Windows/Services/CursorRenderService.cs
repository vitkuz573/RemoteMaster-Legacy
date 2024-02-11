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
    private Rectangle? _cachedScreenBounds;
    private readonly uint _cursorInfoSize;

    public event Func<Rectangle> RequestScreenBounds;

    public CursorRenderService()
    {
        _cursorInfoSize = (uint)Marshal.SizeOf(typeof(CURSORINFO));
    }

    public void DrawCursor(Graphics g)
    {
        ArgumentNullException.ThrowIfNull(g);

        var cursorInfo = GetCursorInformation();

        if (cursorInfo.flags != CURSORINFO_FLAGS.CURSOR_SHOWING)
        {
            return;
        }

        var currentScreenBounds = _cachedScreenBounds ?? RequestScreenBounds.Invoke();

        var relativeX = cursorInfo.ptScreenPos.X - currentScreenBounds.Left;
        var relativeY = cursorInfo.ptScreenPos.Y - currentScreenBounds.Top;

        if (relativeX < 0 || relativeX >= currentScreenBounds.Width || relativeY < 0 || relativeY >= currentScreenBounds.Height)
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

    public void UpdateScreenBounds(Rectangle newBounds)
    {
        _cachedScreenBounds = newBounds;
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
