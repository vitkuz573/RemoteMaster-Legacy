// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;
using RemoteMaster.Shared.Models;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class GdiCapturer : ScreenCapturerService
{
    private Bitmap _bitmap;
    private readonly ICursorRenderService _cursorRenderService;

    public override Rectangle CurrentScreenBounds { get; protected set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

    public override Rectangle VirtualScreenBounds { get; protected set; } = SystemInformation.VirtualScreen;

    public override string SelectedScreen { get; protected set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    protected override bool HasMultipleScreens => Screen.AllScreens.Length > 1;

    public GdiCapturer(ICursorRenderService cursorRenderService, IDesktopService desktopService) : base(desktopService)
    {
        _cursorRenderService = cursorRenderService;
        _cursorRenderService.RequestScreenBounds += () => CurrentScreenBounds;

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
            if (SelectedScreen == VIRTUAL_SCREEN)
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
            Log.Error(ex, "Capturer error in GetFrame.");
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
            Log.Error("Failed to release the device context.");
        }

        if (TrackCursor)
        {
            _cursorRenderService.DrawCursor(memoryGraphics);
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

    public override IEnumerable<Display> GetDisplays()
    {
        var screens = Screen.AllScreens.Select(screen => new Display
        {
            Name = screen.DeviceName,
            IsPrimary = screen.Primary,
            Resolution = screen.Bounds.Size,
        }).ToList();

        if (Screen.AllScreens.Length > 1)
        {
            screens.Add(new Display
            {
                Name = VIRTUAL_SCREEN,
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

        if (displayName == VIRTUAL_SCREEN || Screens.ContainsKey(displayName))
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
        if (SelectedScreen == VIRTUAL_SCREEN)
        {
            CurrentScreenBounds = VirtualScreenBounds;
        }
        else
        {
            CurrentScreenBounds = Screen.AllScreens[Screens[SelectedScreen]].Bounds;
        }

        RaiseScreenChangedEvent(CurrentScreenBounds);

        _cursorRenderService.UpdateScreenBounds(CurrentScreenBounds);
    }

    public override void Dispose()
    {
        base.Dispose();
        _bitmap?.Dispose();
    }
}