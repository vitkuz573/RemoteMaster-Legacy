// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Helpers;
using SkiaSharp;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// Linux screen capturing service that uses X11 via the X11Native helper.
/// </summary>
public class X11CapturingService : ScreenCapturingService
{
    private readonly nint _display;

    /// <summary>
    /// Linux screen capturing service that uses X11 via the X11Native helper.
    /// </summary>
    public X11CapturingService(IAppState appState, IOverlayManagerService overlayManagerService, IScreenProvider screenProvider, ILogger<ScreenCapturingService> logger) : base(appState, overlayManagerService, screenProvider, logger)
    {
        _display = X11Native.XOpenDisplay(string.Empty);

        if (_display == nint.Zero)
        {
            throw new Exception("Unable to open X display");
        }
    }

    /// <summary>
    /// Captures the specified screen area using X11 and encodes it using ImageSharp.
    /// </summary>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="bounds">The bounds of the area to capture.</param>
    /// <param name="imageQuality">The desired image quality (used for JPEG).</param>
    /// <param name="codec">The MIME type of the encoder (e.g., "image/png", "image/jpeg").</param>
    /// <returns>A byte array containing the encoded image.</returns>
    protected override byte[] CaptureScreen(string connectionId, System.Drawing.Rectangle bounds, int imageQuality, string codec)
    {
        ArgumentNullException.ThrowIfNull(codec);

#pragma warning disable CA2000
        var currentFrame = new SKBitmap(bounds.Width, bounds.Height);
#pragma warning restore CA2000

        var window = X11Native.XDefaultRootWindow(_display);

        var imagePointer = X11Native.XGetImage(_display, window, bounds.X, bounds.Y, bounds.Width, bounds.Height, ~0, 2);

        if (imagePointer == nint.Zero)
        {
            return EncodeImage(currentFrame, imageQuality, codec);
        }

        var image = Marshal.PtrToStructure<X11Native.XImage>(imagePointer);

        var pixels = currentFrame.GetPixels();

        unsafe
        {
            var scan1 = (byte*)pixels.ToPointer();
            var scan2 = (byte*)image.data.ToPointer();
            var bytesPerPixel = currentFrame.BytesPerPixel;
            var totalSize = currentFrame.Height * currentFrame.Width * bytesPerPixel;
            
            for (var counter = 0; counter < totalSize - bytesPerPixel; counter++)
            {
                scan1[counter] = scan2[counter];
            }
        }

        Marshal.DestroyStructure<X11Native.XImage>(imagePointer);

        X11Native.XDestroyImage(imagePointer);

        return EncodeImage(currentFrame, imageQuality, codec);
    }

    /// <summary>
    /// Encodes the given Image<Rgba32> into a byte array using the specified codec.
    /// </summary>
    /// <param name="image">The ImageSharp image to encode.</param>
    /// <param name="quality">The quality parameter (used for JPEG encoding).</param>
    /// <param name="codec">The MIME type of the encoder (e.g., "image/png", "image/jpeg").</param>
    /// <returns>A byte array containing the encoded image.</returns>
    private static byte[] EncodeImage(SKBitmap image, int quality, string codec)
    {
        var format = SKEncodedImageFormat.Png;

        if (codec.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            format = SKEncodedImageFormat.Jpeg;
        }
        else if (codec.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            format = SKEncodedImageFormat.Png;
        }

        using var skImage = SKImage.FromBitmap(image);

        using var data = skImage.Encode(format, quality);

        return data.ToArray();
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_display != nint.Zero)
        {
            X11Native.XCloseDisplay(_display);
        }
    }
}
