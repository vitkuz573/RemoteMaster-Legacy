// Originally licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licensed the original code under the MIT license.
//
// Adapted by Vitaly Kuzyaev.
// This adapted version is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Windows.Win32;

public static unsafe partial class PInvoke
{
    public delegate bool EnumDisplayMonitorsCallback(HMONITOR monitor, HDC hdc);

    private static readonly delegate* unmanaged[Stdcall]<HMONITOR, HDC, RECT*, LPARAM, BOOL> s_enumDisplayMonitorsThunk = &EnumDisplayMonitorsThunk;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static BOOL EnumDisplayMonitorsThunk(HMONITOR monitor, HDC hdc, RECT* lprcMonitor, LPARAM lParam)
    {
        var handle = GCHandle.FromIntPtr(lParam);
        var callback = (EnumDisplayMonitorsCallback)handle.Target!;

        return callback(monitor, hdc);
    }

    public static BOOL EnumDisplayMonitors(EnumDisplayMonitorsCallback callBack)
    {
        var gcHandle = GCHandle.Alloc(callBack);

        try
        {
            return EnumDisplayMonitors(default, (RECT?)null, s_enumDisplayMonitorsThunk, (nint)gcHandle);
        }
        finally
        {
            gcHandle.Free();
        }
    }
}
