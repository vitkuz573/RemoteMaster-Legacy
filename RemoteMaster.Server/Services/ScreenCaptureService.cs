using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class ScreenCaptureService : IScreenCaptureService
{
    private readonly ConcurrentDictionary<string, ClientConfig> _clientConfigs = new();

    public unsafe byte[] CaptureScreen()
    {
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

    private static byte[] SaveBitmap(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);

        return memoryStream.ToArray();
    }

    public ClientConfig GetClientConfig(string ipAddress)
    {
        if (!_clientConfigs.TryGetValue(ipAddress, out var config))
        {
            config = new ClientConfig();
            _clientConfigs[ipAddress] = config;
        }

        return config;
    }
}