using Microsoft.IO;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Native.Windows;
using RemoteMaster.Shared.Native.Windows.ScreenHelper;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class ScreenCapturer : IScreenCapturer
{
    private readonly RecyclableMemoryStreamManager _recycleManager = new();

    private readonly Dictionary<string, int> _bitBltScreens = new();

    public Rectangle CurrentScreenBounds { get;private set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;
    public string SelectedScreen { get; private set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    public event EventHandler<Rectangle>? ScreenChanged;

    public ScreenCapturer()
    {
        InitBitBlt();
    }

    public unsafe byte[] CaptureScreen()
    {
        DesktopHelper.SwitchToInputDesktop();

        var width = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN);
        var height = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN);

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var memoryGraphics = Graphics.FromImage(bitmap);

        var dc1 = GetDC(HWND.Null);
        var dc2 = (HDC)memoryGraphics.GetHdc();

        BitBlt(dc2, 0, 0, width, height, dc1, 0, 0, ROP_CODE.SRCCOPY);

        memoryGraphics.ReleaseHdc(dc2);
        ReleaseDC(HWND.Null, dc1);

        return SaveBitmap(bitmap);
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
            data = EncodeBitmap(skBitmap, 10);
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
