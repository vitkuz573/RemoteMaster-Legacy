// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Rectangle = System.Drawing.Rectangle;

namespace RemoteMaster.Host.Linux.Services;

public class X11CapturingService : ScreenCapturingService
{
    private readonly nint _display;

    public X11CapturingService(IAppState appState, IOverlayManagerService overlayManagerService, IScreenProvider screenProvider, ILogger<ScreenCapturingService> logger) : base(appState, overlayManagerService, screenProvider, logger)
    {
        _display = X11Native.XOpenDisplay(string.Empty);

        if (_display == nint.Zero)
        {
            throw new Exception("Unable to open X display");
        }
    }

    protected override byte[] CaptureScreen(string connectionId, Rectangle bounds, int imageQuality, string codec)
    {
        ArgumentNullException.ThrowIfNull(codec);

        using var currentFrame = new Image<Rgba32>(bounds.Width, bounds.Height);

        var window = X11Native.XDefaultRootWindow(_display);
        var imagePointer = X11Native.XGetImage(_display, window, bounds.X, bounds.Y, bounds.Width, bounds.Height, ~0, 2);

        if (imagePointer == nint.Zero)
        {
            return EncodeImage(currentFrame, imageQuality, codec);
        }

        var xImage = Marshal.PtrToStructure<X11Native.XImage>(imagePointer);

        if (xImage.bits_per_pixel != 32 || xImage.depth != 24)
        {
            Marshal.DestroyStructure<X11Native.XImage>(imagePointer);

            X11Native.XDestroyImage(imagePointer);

            throw new Exception($"Unsupported XImage format: bits_per_pixel={xImage.bits_per_pixel}, depth={xImage.depth}");
        }

        if (xImage.red_mask != 0xff0000UL || xImage.green_mask != 0xff00UL || xImage.blue_mask != 0xffUL)
        {
            Marshal.DestroyStructure<X11Native.XImage>(imagePointer);

            X11Native.XDestroyImage(imagePointer);

            throw new Exception($"Unexpected channel masks: red_mask=0x{xImage.red_mask:X}, green_mask=0x{xImage.green_mask:X}, blue_mask=0x{xImage.blue_mask:X}");
        }

        if (currentFrame.DangerousTryGetSinglePixelMemory(out var pixelMemory))
        {
            unsafe
            {
                var destPixels = pixelMemory.Span;
                var pixelCount = bounds.Width * bounds.Height;
                var srcPixels = new ReadOnlySpan<uint>(xImage.data.ToPointer(), pixelCount);

                var swapRedBlue = xImage.byte_order == 0;
                
                for (var i = 0; i < pixelCount; i++)
                {
                    var pixel = srcPixels[i];
                    
                    if (swapRedBlue)
                    {
                        var blue = (byte)(pixel & 0xff);
                        var green = (byte)((pixel >> 8) & 0xff);
                        var red = (byte)((pixel >> 16) & 0xff);

                        destPixels[i] = new Rgba32(red, green, blue, 255);
                    }
                    else
                    {
                        var red = (byte)((pixel >> 16) & 0xff);
                        var green = (byte)((pixel >> 8) & 0xff);
                        var blue = (byte)(pixel & 0xff);

                        destPixels[i] = new Rgba32(red, green, blue, 255);
                    }
                }
            }
        }
        else
        {
            for (var y = 0; y < currentFrame.Height; y++)
            {
                var rowSpan = currentFrame.DangerousGetPixelRowMemory(y);

                unsafe
                {
                    var src = (byte*)xImage.data.ToPointer() + y * bounds.Width * sizeof(uint);
                    var swapRedBlue = xImage.byte_order == 0;
                    
                    for (var x = 0; x < bounds.Width; x++)
                    {
                        var pixel = *((uint*)src + x);
                        
                        if (swapRedBlue)
                        {
                            var blue = (byte)(pixel & 0xff);
                            var green = (byte)((pixel >> 8) & 0xff);
                            var red = (byte)((pixel >> 16) & 0xff);
                            
                            rowSpan.Span[x] = new Rgba32(red, green, blue, 255);
                        }
                        else
                        {
                            var red = (byte)((pixel >> 16) & 0xff);
                            var green = (byte)((pixel >> 8) & 0xff);
                            var blue = (byte)(pixel & 0xff);
                            
                            rowSpan.Span[x] = new Rgba32(red, green, blue, 255);
                        }
                    }
                }
            }
        }

        Marshal.DestroyStructure<X11Native.XImage>(imagePointer);
        X11Native.XDestroyImage(imagePointer);

        return EncodeImage(currentFrame, imageQuality, codec);
    }

    private static byte[] EncodeImage(Image<Rgba32> image, int quality, string codec)
    {
        using var ms = new MemoryStream();

        if (codec.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            var encoder = new JpegEncoder { Quality = quality };
            image.Save(ms, encoder);
        }
        else if (codec.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            var encoder = new PngEncoder();
            image.Save(ms, encoder);
        }
        else
        {
            var encoder = new PngEncoder();
            image.Save(ms, encoder);
        }

        return ms.ToArray();
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
