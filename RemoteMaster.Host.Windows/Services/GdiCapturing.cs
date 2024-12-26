// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class GdiCapturing(IAppState appState, IDesktopService desktopService, IOverlayManagerService overlayManagerService, IScreenProvider screenProvider, ILogger<ScreenCapturingService> logger) : ScreenCapturingService(appState, desktopService, overlayManagerService, screenProvider, logger)
{
    private Bitmap? _bitmap;
    private Graphics? _memoryGraphics;

    private readonly IOverlayManagerService _overlayManagerService = overlayManagerService;

    protected override byte[] CaptureScreen(string connectionId, Rectangle bounds, int imageQuality, string codec)
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

    public override void Dispose()
    {
        base.Dispose();

        _bitmap?.Dispose();
        _memoryGraphics?.Dispose();
    }
}
