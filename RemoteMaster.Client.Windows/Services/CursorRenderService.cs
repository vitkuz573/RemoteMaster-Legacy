// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.InteropServices;
using RemoteMaster.Client.Abstractions;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Client.Services;

public class CursorRenderService : ICursorRenderService
{
    private Point? _lastCursorPoint;
    private Icon? _lastCursorIcon;
    private Rectangle? _cachedScreenBounds;

    public event Func<Rectangle> RequestScreenBounds;

    public void DrawCursor(Graphics g)
    {
        if (g == null)
        {
            throw new ArgumentNullException(nameof(g));
        }

        var cursorInfo = GetCursorInformation();

        if (cursorInfo.flags == CURSORINFO_FLAGS.CURSOR_SHOWING)
        {
            var currentScreenBounds = _cachedScreenBounds ?? RequestScreenBounds?.Invoke();

            if (currentScreenBounds.HasValue)
            {
                var relativeX = cursorInfo.ptScreenPos.X - currentScreenBounds.Value.Left;
                var relativeY = cursorInfo.ptScreenPos.Y - currentScreenBounds.Value.Top;

                if (relativeX >= 0 && relativeX < currentScreenBounds.Value.Width && relativeY >= 0 && relativeY < currentScreenBounds.Value.Height)
                {
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
            }
        }
    }

    public void UpdateScreenBounds(Rectangle newBounds)
    {
        _cachedScreenBounds = newBounds;
    }

    private static CURSORINFO GetCursorInformation()
    {
        var cursorInfo = new CURSORINFO
        {
            cbSize = (uint)Marshal.SizeOf(typeof(CURSORINFO))
        };

        GetCursorInfo(ref cursorInfo);

        return cursorInfo;
    }

    public void Dispose()
    {
        _lastCursorIcon?.Dispose();
    }
}