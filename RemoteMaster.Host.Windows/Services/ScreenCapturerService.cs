// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.IO;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace RemoteMaster.Host.Windows.Services;

public abstract class ScreenCapturerService : IScreenCapturerService
{
    protected const string VIRTUAL_SCREEN = "VIRTUAL_SCREEN";

    private readonly RecyclableMemoryStreamManager _recycleManager = new();
    private readonly IDesktopService _desktopService;
    private readonly object _screenBoundsLock = new();
    private int _quality = 25;

    public bool TrackCursor { get; set; } = false;

    public virtual Dictionary<string, int> Screens { get; } = new();

    public abstract Rectangle CurrentScreenBounds { get; protected set; }

    public abstract Rectangle VirtualScreenBounds { get; protected set; }

    public abstract string SelectedScreen { get; protected set; }

    protected abstract bool HasMultipleScreens { get; }

    public int Quality
    {
        get => _quality;
        set
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Quality must be between 0 and 100");
            }

            _quality = value;
        }
    }

    public event EventHandler<Rectangle>? ScreenChanged;

    protected ScreenCapturerService(IDesktopService desktopService)
    {
        _desktopService = desktopService;

        Init();
    }

    protected abstract void Init();

    protected abstract byte[]? GetFrame();

    public abstract IEnumerable<Display> GetDisplays();

    public abstract void SetSelectedScreen(string displayName);

    protected abstract void RefreshCurrentScreenBounds();

    public byte[]? GetNextFrame()
    {
        lock (_screenBoundsLock)
        {
            try
            {
                if (!_desktopService.SwitchToInputDesktop())
                {
                    Log.Error("Failed to switch to input desktop. Last Win32 error code: {ErrorCode}", Marshal.GetLastWin32Error());
                }

                return GetFrame();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting next frame.");

                return null;
            }
        }
    }

    public byte[]? GetThumbnail(int maxWidth, int maxHeight)
    {
        var originalScreen = SelectedScreen;

        // If there are multiple screens, set to VirtualScreenName temporarily
        if (HasMultipleScreens)
        {
            SetSelectedScreen(VIRTUAL_SCREEN);
        }

        var frame = GetNextFrame();

        // Restore the original selected screen
        SetSelectedScreen(originalScreen);

        if (frame == null)
        {
            return null;
        }

        using var fullImage = SKBitmap.Decode(frame);
        var scale = Math.Min((float)maxWidth / fullImage.Width, (float)maxHeight / fullImage.Height);
        var thumbWidth = (int)(fullImage.Width * scale);
        var thumbHeight = (int)(fullImage.Height * scale);

        using var thumbnail = fullImage.Resize(new SKImageInfo(thumbWidth, thumbHeight), SKFilterQuality.High);

        return EncodeBitmap(thumbnail);
    }

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

        var skBitmap = bitmap.ToSKBitmap();

        var data = EncodeBitmap(skBitmap);

        return data;
    }

    protected void RaiseScreenChangedEvent(Rectangle currentScreenBounds)
    {
        ScreenChanged?.Invoke(this, currentScreenBounds);
    }

    public virtual void Dispose()
    {
    }
}