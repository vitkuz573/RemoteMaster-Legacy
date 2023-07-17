using Microsoft.IO;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Native.Windows;
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
    private RecyclableMemoryStreamManager _recycleManager = new();

    public ScreenCapturer()
    {
        DesktopHelper.SwitchToInputDesktop();
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

    private byte[] EncodeBitmap(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
    {
        using var ms = _recycleManager.GetStream();
        bitmap.Encode(ms, format, quality);

        return ms.ToArray();
    }

    private unsafe byte[] SaveBitmap(Bitmap bitmap)
    {
        var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888);

        byte[] data;

        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

        try
        {
            using var newImage = SKImage.FromPixels(info, bitmapData.Scan0);
            var skBitmap = SKBitmap.FromImage(newImage);
            data = EncodeBitmap(skBitmap, SKEncodedImageFormat.Jpeg, 80);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return data;
    }
}
