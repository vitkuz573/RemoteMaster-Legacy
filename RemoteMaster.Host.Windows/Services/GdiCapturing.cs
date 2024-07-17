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

public class GdiCapturing : ScreenCapturingService
{
    private Bitmap _bitmap;
    private Graphics _memoryGraphics;
    private readonly ICursorRenderService _cursorRenderService;

    public override Rectangle CurrentScreenBounds { get; protected set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

    public override Rectangle VirtualScreenBounds { get; } = SystemInformation.VirtualScreen;

    public override string SelectedScreen { get; protected set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    protected override bool HasMultipleScreens => Screen.AllScreens.Length > 1;

    public GdiCapturing(ICursorRenderService cursorRenderService, IDesktopService desktopService) : base(desktopService)
    {
        _cursorRenderService = cursorRenderService;
        _bitmap = new Bitmap(CurrentScreenBounds.Width, CurrentScreenBounds.Height, PixelFormat.Format32bppArgb);
        _memoryGraphics = Graphics.FromImage(_bitmap);
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
            return SelectedScreen == VirtualScreen ? GetVirtualScreenFrame() : GetSingleScreenFrame();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Capturer error in GetFrame.");

            return null;
        }
    }

    private byte[] CaptureScreen(int width, int height, int left, int top)
    {
        if (_bitmap.Width != width || _bitmap.Height != height)
        {
            _bitmap.Dispose();
            _bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _memoryGraphics.Dispose();
            _memoryGraphics = Graphics.FromImage(_bitmap);
        }

        var dc1 = GetDC(HWND.Null);
        var dc2 = (HDC)_memoryGraphics.GetHdc();

        BitBlt(dc2, 0, 0, width, height, dc1, left, top, ROP_CODE.SRCCOPY);

        _memoryGraphics.ReleaseHdc(dc2);
        ReleaseDC(HWND.Null, dc1);

        if (DrawCursor)
        {
            _cursorRenderService.DrawCursor(_memoryGraphics, CurrentScreenBounds);
        }

        return UseSkia ? SaveBitmap(_bitmap) : BitmapToByteArray(_bitmap);
    }

    private byte[] GetVirtualScreenFrame()
    {
        return CaptureScreen(VirtualScreenBounds.Width, VirtualScreenBounds.Height, VirtualScreenBounds.Left, VirtualScreenBounds.Top);
    }

    private byte[] GetSingleScreenFrame()
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
                Name = VirtualScreen,
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

        if (displayName == VirtualScreen || Screens.ContainsKey(displayName))
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
        CurrentScreenBounds = SelectedScreen == VirtualScreen ? VirtualScreenBounds : Screen.AllScreens[Screens[SelectedScreen]].Bounds;
        RaiseScreenChangedEvent(CurrentScreenBounds);
        _cursorRenderService.ClearCache();
    }

    public override void Dispose()
    {
        base.Dispose();
        _bitmap.Dispose();
        _memoryGraphics.Dispose();
    }

    private static byte[] BitmapToByteArray(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);

        return memoryStream.ToArray();
    }
}
