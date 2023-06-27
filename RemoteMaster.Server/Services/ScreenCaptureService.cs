using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class ScreenCaptureService : IScreenCaptureService
{
    private readonly ConcurrentDictionary<string, ClientConfig> _clientConfigs = new();

    public byte[] CaptureScreen()
    {
        var width = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN);
        var height = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN);

        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
        }

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