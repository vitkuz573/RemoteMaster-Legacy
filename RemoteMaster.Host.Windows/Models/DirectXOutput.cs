// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Dxgi.Common;

namespace RemoteMaster.Host.Windows.Models;

public class DirectXOutput : IDisposable
{
    public Rectangle Bounds { get; }

    public IDXGIAdapter1 Adapter { get; }

    public ID3D11Device Device { get; }

    public IDXGIOutputDuplication OutputDuplication { get; }

    public ID3D11Texture2D Texture2D { get; }

    public DXGI_MODE_ROTATION Rotation { get; }

    private bool _disposed;

    public DirectXOutput(IDXGIAdapter1 adapter, ID3D11Device device, IDXGIOutputDuplication outputDuplication, ID3D11Texture2D texture2D, DXGI_MODE_ROTATION rotation)
    {
        ArgumentNullException.ThrowIfNull(texture2D);

        Adapter = adapter;
        Device = device;
        OutputDuplication = outputDuplication;
        Texture2D = texture2D;
        Rotation = rotation;

        var desc = GetTexture2DDesc(texture2D);
        Bounds = new Rectangle(0, 0, (int)desc.Width, (int)desc.Height);
    }

    private static D3D11_TEXTURE2D_DESC GetTexture2DDesc(ID3D11Texture2D texture2D)
    {
        D3D11_TEXTURE2D_DESC desc;

        unsafe
        {
            texture2D.GetDesc(&desc);
        }

        return desc;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (OutputDuplication != null)
        {
            Marshal.ReleaseComObject(OutputDuplication);
        }

        if (Device != null)
        {
            Marshal.ReleaseComObject(Device);
        }

        if (Texture2D != null)
        {
            Marshal.ReleaseComObject(Texture2D);
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DirectXOutput()
    {
        Dispose(false);
    }
}