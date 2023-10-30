// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.IO;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using SkiaSharp;

namespace RemoteMaster.Host.Windows.Services;

public abstract class ScreenCapturerService : IScreenCapturerService
{
    private readonly RecyclableMemoryStreamManager _recycleManager = new();
    private readonly IDesktopService _desktopService;
    protected readonly ILogger<ScreenCapturerService> _logger;
    private readonly object _screenBoundsLock = new();
    private readonly object _bitmapLock = new();
    private SKBitmap _skBitmap;
    private int _quality = 25;

    public bool TrackCursor { get; set; } = false;

    public virtual Dictionary<string, int> Screens { get; } = new();

    public abstract Rectangle CurrentScreenBounds { get; protected set; }

    public abstract Rectangle VirtualScreenBounds { get; protected set; }

    public abstract string SelectedScreen { get; protected set; }

    protected abstract bool HasMultipleScreens { get; }

    protected abstract string VirtualScreenName { get; }

    public int Quality
    {
        get => _quality;
        set
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentException("Quality must be between 0 and 100");
            }

            _quality = value;
        }
    }

    public event EventHandler<Rectangle>? ScreenChanged;

    protected ScreenCapturerService(IDesktopService desktopService, ILogger<ScreenCapturerService> logger)
    {
        _desktopService = desktopService;
        _logger = logger;
        Init();
    }

    protected abstract void Init();

    public byte[]? GetNextFrame(int? maxWidth = null, int? maxHeight = null, bool isThumbnail = false)
    {
        lock (_screenBoundsLock)
        {
            try
            {
                if (!_desktopService.SwitchToInputDesktop())
                {
                    var errCode = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to switch to input desktop. Last Win32 error code: {errCode}", errCode);
                }

                var originalScreen = SelectedScreen;

                // If there are multiple screens and a thumbnail is being created, set to VirtualScreenName temporarily
                if (HasMultipleScreens && isThumbnail)
                {
                    SetSelectedScreen(VirtualScreenName);
                }

                var result = GetFrame();

                // Restore the original selected screen
                SetSelectedScreen(originalScreen);

                if (maxWidth.HasValue && maxHeight.HasValue && result != null)
                {
                    using var fullImage = SKBitmap.Decode(result);
                    var scale = Math.Min((float)maxWidth.Value / fullImage.Width, (float)maxHeight.Value / fullImage.Height);
                    var thumbWidth = (int)(fullImage.Width * scale);
                    var thumbHeight = (int)(fullImage.Height * scale);

                    using var thumbnail = fullImage.Resize(new SKImageInfo(thumbWidth, thumbHeight), SKFilterQuality.High);

                    result = EncodeBitmap(thumbnail);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting next frame.");

                return null;
            }
        }
    }

    protected abstract byte[]? GetFrame();

    public abstract IEnumerable<DisplayInfo> GetDisplays();

    public abstract void SetSelectedScreen(string displayName);

    protected abstract void RefreshCurrentScreenBounds();

    protected byte[] EncodeBitmap(SKBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        using var ms = _recycleManager.GetStream();

        var encoderOptions = new SKJpegEncoderOptions
        {
            Quality = Quality,
            Downsample = SKJpegEncoderDownsample.Downsample420
        };

        using var pixmap = bitmap.PeekPixels();
        using var data = pixmap.Encode(encoderOptions);
        data.SaveTo(ms);

        return ms.ToArray();
    }

    protected byte[] SaveBitmap(Bitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888);

        byte[] data;

        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

        try
        {
            lock (_bitmapLock)
            {
                _skBitmap ??= new SKBitmap(info);
                _skBitmap.InstallPixels(info, bitmapData.Scan0, bitmapData.Stride);
                data = EncodeBitmap(_skBitmap);
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return data;
    }

    protected void RaiseScreenChangedEvent(Rectangle currentScreenBounds)
    {
        ScreenChanged?.Invoke(this, currentScreenBounds);
    }

    public virtual void Dispose()
    {
        //
    }
}
