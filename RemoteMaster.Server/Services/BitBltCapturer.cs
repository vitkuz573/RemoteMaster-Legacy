// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Native.Windows.ScreenHelper;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class BitBltCapturer : ScreenCapturer
{
    private Bitmap _bitmap;
    private Point? _lastCursorPoint;
    private Icon? _lastCursorIcon;

    public override Rectangle CurrentScreenBounds { get; protected set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

    public override Rectangle VirtualScreenBounds { get; protected set; } = SystemInformation.VirtualScreen;

    public override string SelectedScreen { get; protected set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    public BitBltCapturer(ILogger<ScreenCapturer> logger) : base(logger)
    {
        _bitmap = new Bitmap(CurrentScreenBounds.Width, CurrentScreenBounds.Height, PixelFormat.Format32bppArgb);
    }

    protected override void Init()
    {
        Screens.Clear();

        for (var i = 0; i < Screen.AllScreens.Length; i++)
        {
            Screens.Add(Screen.AllScreens[i].DeviceName, i);
        }
    }

    protected override byte[]? GetFrame()
    {
        try
        {
            if (SelectedScreen == "VIRTUAL_SCREEN")
            {
                return GetVirtualScreenFrame();
            }
            else
            {
                return GetSingleScreenFrame();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capturer error in GetFrame.");
            return null;
        }
    }

    private byte[]? CaptureScreen(int width, int height, int left, int top)
    {
        if (_bitmap.Width != width || _bitmap.Height != height)
        {
            _bitmap.Dispose();
            _bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        }

        using var memoryGraphics = Graphics.FromImage(_bitmap);

        var dc1 = GetDC(HWND.Null);
        var dc2 = (HDC)memoryGraphics.GetHdc();

        BitBlt(dc2, 0, 0, width, height, dc1, left, top, ROP_CODE.SRCCOPY);

        memoryGraphics.ReleaseHdc(dc2);
        ReleaseDC(HWND.Null, dc1);

        var cursorInfo = GetCursorInfo();
        DrawCursor(memoryGraphics, cursorInfo);

        return SaveBitmap(_bitmap);
    }

    private static CURSORINFO GetCursorInfo()
    {
        var cursorInfo = new CURSORINFO();
        cursorInfo.cbSize = (uint)Marshal.SizeOf(cursorInfo);

        PInvoke.GetCursorInfo(ref cursorInfo);

        return cursorInfo;
    }


    private void DrawCursor(Graphics g, CURSORINFO cursorInfo)
    {
        if (!TrackCursor)
        {
            return;
        }

        if (cursorInfo.flags == CURSORINFO_FLAGS.CURSOR_SHOWING)
        {
            var relativeX = cursorInfo.ptScreenPos.X - CurrentScreenBounds.Left;
            var relativeY = cursorInfo.ptScreenPos.Y - CurrentScreenBounds.Top;

            if (relativeX >= 0 && relativeX < CurrentScreenBounds.Width && relativeY >= 0 && relativeY < CurrentScreenBounds.Height)
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

    private byte[]? GetVirtualScreenFrame()
    {
        return CaptureScreen(VirtualScreenBounds.Width, VirtualScreenBounds.Height, VirtualScreenBounds.Left, VirtualScreenBounds.Top);
    }

    private byte[]? GetSingleScreenFrame()
    {
        return CaptureScreen(CurrentScreenBounds.Width, CurrentScreenBounds.Height, CurrentScreenBounds.Left, CurrentScreenBounds.Top);
    }

    public override IEnumerable<DisplayInfo> GetDisplays()
    {
        var screens = Screen.AllScreens.Select(screen => new DisplayInfo
        {
            Name = screen.DeviceName,
            IsPrimary = screen.Primary,
            Resolution = screen.Bounds.Size,
        }).ToList();

        if (Screen.AllScreens.Length > 1)
        {
            screens.Add(new DisplayInfo
            {
                Name = "VIRTUAL_SCREEN",
                IsPrimary = false,
                Resolution = new Size(VirtualScreenBounds.Width, VirtualScreenBounds.Height),
            });
        }

        return screens;
    }

    public override void SetSelectedScreen(string displayName)
    {
        if (displayName == SelectedScreen)
        {
            return;
        }

        if (displayName == "VIRTUAL_SCREEN" || Screens.ContainsKey(displayName))
        {
            SelectedScreen = displayName;
        }
        else
        {
            SelectedScreen = Screens.Keys.First();
        }

        RefreshCurrentScreenBounds();
    }

    protected override void RefreshCurrentScreenBounds()
    {
        if (SelectedScreen == "VIRTUAL_SCREEN")
        {
            CurrentScreenBounds = VirtualScreenBounds;
        }
        else
        {
            CurrentScreenBounds = Screen.AllScreens[Screens[SelectedScreen]].Bounds;
        }

        RaiseScreenChangedEvent(CurrentScreenBounds);
    }

    public override void Dispose()
    {
        base.Dispose();
        _bitmap?.Dispose();
        _lastCursorIcon?.Dispose();
    }
}