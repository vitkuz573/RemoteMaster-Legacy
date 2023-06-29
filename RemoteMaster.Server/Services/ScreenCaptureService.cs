using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.DXGI.Resource;

namespace RemoteMaster.Server.Services;

public class ScreenCaptureService : IScreenCaptureService
{
    private readonly ConcurrentDictionary<string, ClientConfig> _clientConfigs = new();
    private readonly Device _device;
    private readonly OutputDuplication _duplicatedOutput;
    private bool _isDirectXEnabled = true;

    public ScreenCaptureService()
    {
        var factory = new Factory1();
        var adapter = factory.GetAdapter1(0);
        _device = new Device(adapter);
        var output = adapter.GetOutput(0);
        var output1 = output.QueryInterface<Output1>();
        _duplicatedOutput = output1.DuplicateOutput(_device);
    }

    public unsafe byte[] CaptureScreen()
    {
        return CaptureScreenWithBitBlt();

        // try
        // {
        //     if (_isDirectXEnabled)
        //     {
        //         return CaptureScreenWithDirectX();
        //     }
        //     else
        //     {
        //         return CaptureScreenWithBitBlt();
        //     }
        // }
        // catch
        // {
        //     _isDirectXEnabled = false;
        // 
        //     return CaptureScreenWithBitBlt();
        // }
    }

    private unsafe byte[] CaptureScreenWithDirectX()
    {
        var result = _duplicatedOutput.TryAcquireNextFrame(1000, out _, out Resource screenResource);

        if (result.Failure || screenResource == null)
        {
            throw new Exception("Error capturing screen with DirectX.");
        }

        var screenTexture = screenResource.QueryInterface<Texture2D>();
        var dataBox = _device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, MapFlags.None);

        var width = screenTexture.Description.Width;
        var height = screenTexture.Description.Height;
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var boundsRect = new Rectangle(0, 0, width, height);

        var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

        Unsafe.CopyBlock((void*)mapDest.Scan0, (void*)dataBox.DataPointer, (uint)(width * height * 4));

        bitmap.UnlockBits(mapDest);
        _device.ImmediateContext.UnmapSubresource(screenTexture, 0);

        return SaveBitmap(bitmap);
    }

    private static byte[] CaptureScreenWithBitBlt()
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