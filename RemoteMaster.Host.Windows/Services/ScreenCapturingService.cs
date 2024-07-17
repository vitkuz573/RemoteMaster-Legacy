// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Buffers;
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

public abstract class ScreenCapturingService : IScreenCapturingService
{
    protected const string VirtualScreen = "VIRTUAL_SCREEN";

    private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Shared;

    private readonly RecyclableMemoryStreamManager _recycleManager = new();
    private readonly IDesktopService _desktopService;
    private readonly object _screenBoundsLock = new();

    public bool DrawCursor { get; set; } = false;

    public int ImageQuality { get; set; } = 25;

    public bool UseSkia { get; set; } = false;

    public string? SelectedCodec { get; set; } = "image/jpeg";

    protected Dictionary<string, int> Screens { get; } = [];

    public abstract Rectangle CurrentScreenBounds { get; protected set; }

    public abstract Rectangle VirtualScreenBounds { get; }

    public abstract string SelectedScreen { get; protected set; }

    protected abstract bool HasMultipleScreens { get; }

    public event EventHandler<Rectangle>? ScreenChanged;

    protected ScreenCapturingService(IDesktopService desktopService)
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
                RefreshCurrentScreenBounds();

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

        if (HasMultipleScreens)
        {
            SetSelectedScreen(VirtualScreen);
        }

        var frame = GetNextFrame();

        SetSelectedScreen(originalScreen);

        if (frame == null)
        {
            return null;
        }

        if (!UseSkia)
        {
            return frame;
        }

        using var fullImage = SKBitmap.Decode(frame);
        var scale = Math.Min((float)maxWidth / fullImage.Width, (float)maxHeight / fullImage.Height);
        var thumbWidth = (int)(fullImage.Width * scale);
        var thumbHeight = (int)(fullImage.Height * scale);

        using var thumbnail = fullImage.Resize(new SKImageInfo(thumbWidth, thumbHeight), SKFilterQuality.High);

        return EncodeBitmap(thumbnail);
    }

    private byte[] EncodeBitmap(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var ms = _recycleManager.GetStream();
        using var pixmap = bitmap.PeekPixels();
        using var data = pixmap.Encode(SKEncodedImageFormat.Jpeg, ImageQuality);

        var buffer = ArrayPool.Rent((int)data.Size);

        try
        {
            using var dataStream = data.AsStream();
            var totalBytesRead = 0;

            while (totalBytesRead < data.Size)
            {
                var bytesRead = dataStream.Read(buffer, totalBytesRead, (int)data.Size - totalBytesRead);

                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
            }

            ms.Write(buffer, 0, totalBytesRead);
        }
        finally
        {
            ArrayPool.Return(buffer);
        }

        return ms.ToArray();
    }

    protected byte[] SaveBitmap(Bitmap bitmap)
    {
        var skBitmap = bitmap.ToSKBitmap();

        return EncodeBitmap(skBitmap);
    }

    protected void RaiseScreenChangedEvent(Rectangle currentScreenBounds)
    {
        ScreenChanged?.Invoke(this, currentScreenBounds);
    }

    public virtual void Dispose()
    {
    }
}
