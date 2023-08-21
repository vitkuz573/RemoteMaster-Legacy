// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Native.Windows.ScreenHelper;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class BitBltCapturer : ScreenCapturer
{
    private const string VIRTUAL_SCREEN_NAME = "VIRTUAL_SCREEN";

    private Bitmap _bitmap;

    public override Rectangle CurrentScreenBounds { get; protected set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

    public override Rectangle VirtualScreenBounds { get; protected set; } = SystemInformation.VirtualScreen;

    public override string SelectedScreen { get; protected set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    private readonly ICursorRenderer _cursorRenderer;

    public BitBltCapturer(ICursorRenderer cursorRenderer, ILogger<ScreenCapturer> logger) : base(logger)
    {
        _cursorRenderer = cursorRenderer;
        _cursorRenderer.RequestScreenBounds += () => CurrentScreenBounds;

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
            if (SelectedScreen == VIRTUAL_SCREEN_NAME)
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

        var result = ReleaseDC(HWND.Null, dc1);

        if (result == 0)
        {
            _logger.LogError("Failed to release the device context.");
        }

        if (TrackCursor)
        {
            _cursorRenderer.DrawCursor(memoryGraphics);
        }

        return SaveBitmap(_bitmap);
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
                Name = VIRTUAL_SCREEN_NAME,
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

        if (displayName == VIRTUAL_SCREEN_NAME || Screens.ContainsKey(displayName))
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
        if (SelectedScreen == VIRTUAL_SCREEN_NAME)
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
    }
}