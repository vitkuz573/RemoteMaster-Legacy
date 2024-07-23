// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class DirectXCapturing : ScreenCapturingService
{
    private Bitmap _bitmap;
    private Graphics _memoryGraphics;
    private readonly ICursorRenderService _cursorRenderService;

    private readonly Dictionary<string, DirectXOutput> _directxScreens = [];

    private static readonly D3D_FEATURE_LEVEL[] FeatureLevelsArray =
    [
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_1,
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_0,
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_3,
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_2,
        D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_1
    ];

    private static ReadOnlySpan<D3D_FEATURE_LEVEL> FeatureLevels => FeatureLevelsArray;

    public DirectXCapturing(ICursorRenderService cursorRenderService, IDesktopService desktopService) : base(desktopService)
    {
        _cursorRenderService = cursorRenderService;
        _bitmap = new Bitmap(CurrentScreenBounds.Width, CurrentScreenBounds.Height, PixelFormat.Format32bppArgb);
        _memoryGraphics = Graphics.FromImage(_bitmap);
    }

    private void ClearOutputs()
    {
        foreach (var screen in _directxScreens.Values)
        {
            try
            {
                screen.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        _directxScreens.Clear();
    }

    public override void SetSelectedScreen(string displayName)
    {
        if (displayName == SelectedScreen)
        {
            return;
        }

        if (displayName == VirtualScreen || _directxScreens.ContainsKey(displayName))
        {
            SelectedScreen = displayName;
        }
        else
        {
            SelectedScreen = _directxScreens.Keys.First();
        }
    }

    protected override byte[]? GetFrame()
    {
        try
        {
            return SelectedScreen == VirtualScreen ? GetVirtualScreenFrame() : GetSingleScreenFrame();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Capturing error in GetFrame: {ex.Message}");

            return null;
        }
    }

    private byte[]? GetSingleScreenFrame()
    {
        if (_directxScreens.TryGetValue(SelectedScreen, out var dxOutput))
        {
            return CaptureScreen(dxOutput);
        }

        Console.WriteLine("DirectX output not found.");

        return null;

    }

    private byte[]? GetVirtualScreenFrame()
    {
        if (_directxScreens.Count == 0)
        {
            Console.WriteLine("No DirectX outputs found.");

            return null;
        }

        var totalWidth = _directxScreens.Values.Sum(dx => dx.Bounds.Width);
        var maxHeight = _directxScreens.Values.Max(dx => dx.Bounds.Height);

        using var finalBitmap = new Bitmap(totalWidth, maxHeight, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(finalBitmap))
        {
            graphics.Clear(Color.Black);

            var currentX = 0;

            foreach (var screenBytes in _directxScreens.Values.Select(CaptureScreen).OfType<byte[]>())
            {
                using var ms = new MemoryStream(screenBytes);
                using var screenBitmap = new Bitmap(ms);
                graphics.DrawImage(screenBitmap, currentX, 0);
                currentX += screenBitmap.Width;
            }
        }

        return SaveBitmap(finalBitmap);
    }

    private unsafe byte[]? CaptureScreen(DirectXOutput dxOutput)
    {
        var width = dxOutput.Bounds.Width;
        var height = dxOutput.Bounds.Height;

        var pDevice = dxOutput.Device;
        pDevice.GetImmediateContext(out var immediateContext);

        var duplicatedOutput = dxOutput.OutputDuplication;
        var texture2D = dxOutput.Texture2D;

        var frameInfo = new DXGI_OUTDUPL_FRAME_INFO();

        Thread.Sleep(200);

        duplicatedOutput.AcquireNextFrame(1000, &frameInfo, out var desktopResource);

        var textureGuid = typeof(ID3D11Texture2D).GUID;

        var hr = (HRESULT)Marshal.QueryInterface(Marshal.GetIUnknownForObject(desktopResource), ref textureGuid, out var desktopTexturePtr);

        if (hr.Failed)
        {
            Console.WriteLine($"Failed to query ID3D11Texture2D: 0x{hr.Value:X}");

            return null;
        }

        var desktopTexture = (ID3D11Texture2D)Marshal.GetObjectForIUnknown(desktopTexturePtr);

        immediateContext.CopyResource(texture2D, desktopTexture);

        var mappedResource = new D3D11_MAPPED_SUBRESOURCE();

        try
        {
            immediateContext.Map(texture2D, 0, D3D11_MAP.D3D11_MAP_READ, 0, &mappedResource);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to map texture: {ex.Message}");

            return null;
        }

        byte[]? result = null;

        if (mappedResource.pData != null)
        {
            var dataPtr = mappedResource.pData;
            var stride = mappedResource.RowPitch;

            using var bitmap = new Bitmap(width, height, (int)stride, PixelFormat.Format32bppArgb, (nint)dataPtr);

            switch (dxOutput.Rotation)
            {
                case DXGI_MODE_ROTATION.DXGI_MODE_ROTATION_UNSPECIFIED:
                case DXGI_MODE_ROTATION.DXGI_MODE_ROTATION_IDENTITY:
                    break;
                case DXGI_MODE_ROTATION.DXGI_MODE_ROTATION_ROTATE90:
                    bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    break;
                case DXGI_MODE_ROTATION.DXGI_MODE_ROTATION_ROTATE180:
                    bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    break;
                case DXGI_MODE_ROTATION.DXGI_MODE_ROTATION_ROTATE270:
                    bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            result = SaveBitmap(bitmap);

            immediateContext.Unmap(texture2D, 0);
        }

        if (desktopResource != null)
        {
            Marshal.ReleaseComObject(desktopResource);
        }

        if (desktopTexture != null)
        {
            Marshal.ReleaseComObject(desktopTexture);
        }

        if (immediateContext != null)
        {
            Marshal.ReleaseComObject(immediateContext);
        }

        return result;
    }

    protected override unsafe void Init()
    {
        ClearOutputs();

        var result = CreateDXGIFactory1(typeof(IDXGIFactory1).GUID, out var factory);

        if (result.Failed)
        {
            Console.WriteLine($"Failed to create DXGIFactory1: 0x{result.Value:X}");

            return;
        }

        if (factory is not IDXGIFactory1 dxgiFactory1)
        {
            return;
        }

        uint adapterIndex = 0;

        while (dxgiFactory1.EnumAdapters1(adapterIndex, out var adapter) != HRESULT.DXGI_ERROR_NOT_FOUND)
        {
            uint outputIndex = 0;

            while (adapter.EnumOutputs(outputIndex, out var output) != HRESULT.DXGI_ERROR_NOT_FOUND)
            {
                var outputDesc = output.GetDesc();

                var deviceName = outputDesc.DeviceName.ToString();

                if (deviceName != null)
                {
                    object? adapterObj;
                    var adapterGuid = typeof(IDXGIAdapter1).GUID;

                    var handle = GCHandle.Alloc(adapterGuid, GCHandleType.Pinned);

                    try
                    {
                        var pAdapterGuid = handle.AddrOfPinnedObject();
                        output.GetParent((Guid*)pAdapterGuid, out adapterObj);
                    }
                    finally
                    {
                        handle.Free();
                    }

                    if (adapterObj is IDXGIAdapter1 outputAdapter)
                    {
                        D3D_FEATURE_LEVEL featureLevel;

#pragma warning disable CA2000
                        var hr = D3D11CreateDevice(outputAdapter, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN, new SafeFileHandle(nint.Zero, true), D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT, FeatureLevels, D3D11_SDK_VERSION, out var device, &featureLevel, out var immediateContext);
#pragma warning restore CA2000

                        if (hr.Failed)
                        {
                            Console.WriteLine($"Failed to create D3D11 device: 0x{hr.Value:X}");

                            continue;
                        }

                        Console.WriteLine($"Device created with feature level: {featureLevel}");

                        var output1Guid = typeof(IDXGIOutput1).GUID;

                        hr = (HRESULT)Marshal.QueryInterface(Marshal.GetIUnknownForObject(output), ref output1Guid, out var output1Ptr);

                        if (hr.Failed)
                        {
                            Console.WriteLine($"Failed to query IDXGIOutput1: 0x{hr.Value:X}");

                            continue;
                        }

                        var output1 = (IDXGIOutput1)Marshal.GetObjectForIUnknown(output1Ptr);

                        output1.DuplicateOutput(device, out var duplicatedOutput);

                        var textureDesc = new D3D11_TEXTURE2D_DESC
                        {
                            Width = (uint)outputDesc.DesktopCoordinates.Width,
                            Height = (uint)outputDesc.DesktopCoordinates.Height,
                            MipLevels = 1,
                            ArraySize = 1,
                            Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                            SampleDesc = new DXGI_SAMPLE_DESC
                            {
                                Count = 1,
                                Quality = 0
                            },
                            Usage = D3D11_USAGE.D3D11_USAGE_STAGING,
                            BindFlags = 0,
                            CPUAccessFlags = D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
                            MiscFlags = 0
                        };

                        ID3D11Texture2D_unmanaged* pTexture2D = null;
                        device.CreateTexture2D(&textureDesc, null, &pTexture2D);

                        var stagingTexture = (ID3D11Texture2D?)Marshal.GetObjectForIUnknown((IntPtr)pTexture2D);

                        var directXOutput = new DirectXOutput(outputAdapter, device, duplicatedOutput, stagingTexture, outputDesc.Rotation);

                        _directxScreens.Add(deviceName, directXOutput);
                    }
                    else
                    {
                        Console.WriteLine("Failed to get IDXGIAdapter1 from IDXGIOutput.");
                    }
                }

                Marshal.ReleaseComObject(output);

                outputIndex++;
            }

            Marshal.ReleaseComObject(adapter);

            adapterIndex++;
        }
    }

    protected override void RefreshCurrentScreenBounds()
    {
        if (SelectedScreen == VirtualScreen)
        {
            CurrentScreenBounds = VirtualScreenBounds;
        }
        else if (_directxScreens.TryGetValue(SelectedScreen, out var directXOutput))
        {
            CurrentScreenBounds = directXOutput.Bounds;
        }
        else
        {
            SelectedScreen = _directxScreens.Keys.FirstOrDefault() ?? VirtualScreen;
            CurrentScreenBounds = SelectedScreen == VirtualScreen ? VirtualScreenBounds : _directxScreens[SelectedScreen].Bounds;
        }

        RaiseScreenChangedEvent(CurrentScreenBounds);

        _cursorRenderService.ClearCache();
    }

}