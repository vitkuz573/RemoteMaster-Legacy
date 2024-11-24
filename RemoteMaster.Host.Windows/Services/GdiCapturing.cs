// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class GdiCapturing : ScreenCapturingService
{
    private Bitmap _bitmap;
    private Graphics _memoryGraphics;
    private readonly ILogger<ScreenCapturingService> _logger;

    public GdiCapturing(IEnumerable<IScreenOverlay> screenOverlays, IDesktopService desktopService, ILogger<ScreenCapturingService> logger) : base(desktopService, screenOverlays, logger)
    {
        _logger = logger;
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
            _logger.LogError(ex, "Capturing error in GetFrame.");

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

        foreach (var overlay in GetActiveOverlays())
        {
            overlay.Draw(_memoryGraphics, CurrentScreenBounds);
        }

        return BitmapToByteArray(_bitmap, ImageQuality, SelectedCodec);
    }

    private byte[] GetVirtualScreenFrame()
    {
        return CaptureScreen(VirtualScreenBounds.Width, VirtualScreenBounds.Height, VirtualScreenBounds.Left, VirtualScreenBounds.Top);
    }

    private byte[] GetSingleScreenFrame()
    {
        return CaptureScreen(CurrentScreenBounds.Width, CurrentScreenBounds.Height, CurrentScreenBounds.Left, CurrentScreenBounds.Top);
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
    }

    public override void Dispose()
    {
        base.Dispose();
        _bitmap.Dispose();
        _memoryGraphics.Dispose();
    }

    private static byte[] BitmapToByteArray(Bitmap bitmap, int quality, string? codec)
    {
        using var memoryStream = new MemoryStream();
        using var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

        var imageCodec = GetEncoderInfo(codec);

        if (imageCodec != null)
        {
            bitmap.Save(memoryStream, imageCodec, encoderParameters);
        }
        else
        {
            var pngCodec = GetEncoderInfo("image/png");

            if (pngCodec != null)
            {
                bitmap.Save(memoryStream, pngCodec, null);
            }
            else
            {
                throw new InvalidOperationException("No suitable codec found");
            }
        }

        return memoryStream.ToArray();
    }

    private static ImageCodecInfo? GetEncoderInfo(string? mimeType)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();

        return codecs.FirstOrDefault(codec => codec.MimeType == mimeType);
    }
}
