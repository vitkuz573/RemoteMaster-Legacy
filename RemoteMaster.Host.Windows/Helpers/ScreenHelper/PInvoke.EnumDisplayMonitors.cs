// Originally licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licensed the original code under the MIT license.
//
// Adapted by Vitaly Kuzyaev.
// This adapted version is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Windows.Win32;

[SupportedOSPlatform("windows6.0.6000")]
public static unsafe partial class PInvoke
{
    public delegate bool EnumDisplayMonitorsCallback(HMONITOR monitor, HDC hdc);

    public static BOOL EnumDisplayMonitors(EnumDisplayMonitorsCallback callBack)
    {
        var gcHandle = GCHandle.Alloc(callBack);

        try
        {
            return EnumDisplayMonitors(default, (RECT?)null, EnumDisplayMonitorsNativeCallback, (LPARAM)(nint)gcHandle);
        }
        finally
        {
            gcHandle.Free();
        }
    }

    private static BOOL EnumDisplayMonitorsNativeCallback(HMONITOR monitor, HDC hdc, RECT* lprcMonitor, LPARAM lParam)
    {
        return ((EnumDisplayMonitorsCallback)((GCHandle)(nint)lParam).Target!)(monitor, hdc);
    }
}