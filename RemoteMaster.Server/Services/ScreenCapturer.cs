using Microsoft.IO;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Native.Windows;
using RemoteMaster.Shared.Native.Windows.ScreenHelper;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class ScreenCapturer : IScreenCapturer
{
    private readonly RecyclableMemoryStreamManager _recycleManager = new();
    private readonly Dictionary<string, int> _bitBltScreens = new();
    private readonly object _screenBoundsLock = new();
    private readonly ILogger<ScreenCapturer> _logger;

    public Rectangle CurrentScreenBounds { get;private set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

    public Rectangle VirtualScreenBounds { get; private set; } = SystemInformation.VirtualScreen;
    
    public string SelectedScreen { get; private set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    public event EventHandler<Rectangle>? ScreenChanged;

    public ScreenCapturer(ILogger<ScreenCapturer> logger)
    {
        _logger = logger;

        InitBitBlt();
    }

    public unsafe byte[]? GetBitBltFrame()
    {
        try
        {
            var width = CurrentScreenBounds.Width;
            var height = CurrentScreenBounds.Height;
            var left = CurrentScreenBounds.Left;
            var top = CurrentScreenBounds.Top;

            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var memoryGraphics = Graphics.FromImage(bitmap);

            var dc1 = GetDC(HWND.Null);
            var dc2 = (HDC)memoryGraphics.GetHdc();

            BitBlt(dc2, 0, 0, width, height, dc1, left, top, ROP_CODE.SRCCOPY);

            memoryGraphics.ReleaseHdc(dc2);
            ReleaseDC(HWND.Null, dc1);

            return SaveBitmap(bitmap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capturer error in BitBltCapture.");
            return null;
        }
    }

    public unsafe byte[]? GetNextFrame()
    {
        lock (_screenBoundsLock)
        {
            try
            {
                if (!DesktopHelper.SwitchToInputDesktop())
                {
                    var errCode = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to switch to input desktop. Last Win32 error code: {errCode}", errCode);
                }

                var result = GetBitBltFrame();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting next frame.");
                return null;
            }
        }
    }

    private byte[] EncodeBitmap(SKBitmap bitmap, int quality)
    {
        using var ms = _recycleManager.GetStream();

        var encoderOptions = new SKJpegEncoderOptions
        {
            Quality = quality,
            Downsample = SKJpegEncoderDownsample.Downsample420
        };

        using var pixmap = bitmap.PeekPixels();
        using var data = pixmap.Encode(encoderOptions);
        data.SaveTo(ms);

        return ms.ToArray();
    }

    private unsafe byte[] SaveBitmap(Bitmap bitmap)
    {
        var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888);

        byte[] data;

        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

        try
        {
            var skBitmap = new SKBitmap(info);
            skBitmap.InstallPixels(info, bitmapData.Scan0, bitmapData.Stride);
            data = EncodeBitmap(skBitmap, 80);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return data;
    }

    public IEnumerable<string> GetDisplayNames()
    {
        return Screen.AllScreens.Select(x => x.DeviceName);
    }

    public void SetSelectedScreen(string displayName)
    {
        if (displayName == SelectedScreen)
        {
            return;
        }

        if (_bitBltScreens.ContainsKey(displayName))
        {
            SelectedScreen = displayName;
        }
        else
        {
            SelectedScreen = _bitBltScreens.Keys.First();
        }

        RefreshCurrentScreenBounds();
    }

    private void RefreshCurrentScreenBounds()
    {
        CurrentScreenBounds = Screen.AllScreens[_bitBltScreens[SelectedScreen]].Bounds;
        ScreenChanged?.Invoke(this, CurrentScreenBounds);
    }

    private void InitBitBlt()
    {
        _bitBltScreens.Clear();

        for (var i = 0; i < Screen.AllScreens.Length; i++)
        {
            _bitBltScreens.Add(Screen.AllScreens[i].DeviceName, i);
        }
    }
}
