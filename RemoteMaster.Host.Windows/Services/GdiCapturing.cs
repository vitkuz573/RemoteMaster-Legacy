// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class GdiCapturing : ScreenCapturingService
{
    private Bitmap? _bitmap;
    private Graphics? _memoryGraphics;
    private readonly IAppState _appState;
    private readonly IOverlayManagerService _overlayManagerService;
    private readonly ILogger<ScreenCapturingService> _logger;

    public GdiCapturing(IAppState appState, IDesktopService desktopService, IOverlayManagerService overlayManagerService, ILogger<ScreenCapturingService> logger) : base(appState, desktopService, overlayManagerService, logger)
    {
        _appState = appState;
        _overlayManagerService = overlayManagerService;
        _logger = logger;
    }

    protected override void Init()
    {
        Screens.Clear();

        for (var i = 0; i < Screen.AllScreens.Length; i++)
        {
            Screens.Add(Screen.AllScreens[i].DeviceName, i);
        }
    }

    protected override byte[]? GetFrame(string connectionId)
    {
        try
        {
            _appState.TryGetCapturingContext(connectionId, out var capturingContext);

            if (capturingContext?.SelectedScreen != null)
            {
                return capturingContext.SelectedScreen.DeviceName == Screen.VirtualScreen.DeviceName
                    ? GetVirtualScreenFrame(connectionId, capturingContext.ImageQuality, capturingContext.SelectedCodec)
                    : GetSingleScreenFrame(connectionId, capturingContext.SelectedScreen.Bounds, capturingContext.ImageQuality, capturingContext.SelectedCodec);
            }

            _logger.LogWarning("CapturingContext not found or SelectedScreen is null for connectionId: {ConnectionId}", connectionId);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capturing error in GetFrame.");
            return null;
        }
    }

    private byte[] CaptureScreen(string connectionId, Rectangle bounds, int imageQuality, string codec)
    {
        if (_bitmap == null || _bitmap.Width != bounds.Width || _bitmap.Height != bounds.Height)
        {
            _bitmap?.Dispose();
            _bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

            _memoryGraphics?.Dispose();
            _memoryGraphics = Graphics.FromImage(_bitmap);
        }

        var dc1 = GetDC(HWND.Null);
        var dc2 = (HDC)_memoryGraphics!.GetHdc();

        BitBlt(dc2, 0, 0, bounds.Width, bounds.Height, dc1, bounds.Left, bounds.Top, ROP_CODE.SRCCOPY);

        _memoryGraphics.ReleaseHdc(dc2);
        ReleaseDC(HWND.Null, dc1);

        var activeOverlays = _overlayManagerService.GetActiveOverlays(connectionId);

        foreach (var overlay in activeOverlays)
        {
            overlay.Draw(_memoryGraphics, bounds);
        }

        return BitmapToByteArray(_bitmap, imageQuality, codec);
    }

    private byte[] GetVirtualScreenFrame(string connectionId, int imageQuality, string codec)
    {
        return CaptureScreen(connectionId, Screen.VirtualScreen.Bounds, imageQuality, codec);
    }

    private byte[] GetSingleScreenFrame(string connectionId, Rectangle bounds, int imageQuality, string codec)
    {
        return CaptureScreen(connectionId, bounds, imageQuality, codec);
    }

    public override void SetSelectedScreen(string connectionId, IScreen display)
    {
        _appState.TryGetCapturingContext(connectionId, out var capturingContext);

        if (capturingContext == null)
        {
            _logger.LogError("CapturingContext not found for ConnectionId: {ConnectionId}", connectionId);

            return;
        }

        if (capturingContext.SelectedScreen != null && capturingContext.SelectedScreen.Equals(display))
        {
            return;
        }

        capturingContext.SelectedScreen = display;
    }

    public override void Dispose()
    {
        base.Dispose();

        _bitmap?.Dispose();
        _memoryGraphics?.Dispose();
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
