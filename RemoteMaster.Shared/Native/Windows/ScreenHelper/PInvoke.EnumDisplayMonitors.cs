using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Windows.Win32;

public static unsafe partial class PInvoke
{
    public delegate bool EnumDisplayMonitorsCallback(HMONITOR monitor, HDC hdc);

    public static BOOL EnumDisplayMonitors(EnumDisplayMonitorsCallback callBack)
    {
        GCHandle gcHandle = GCHandle.Alloc(callBack);

        try
        {
            return PInvoke.EnumDisplayMonitors(default, (RECT?)null, EnumDisplayMonitorsNativeCallback, (LPARAM)(nint)gcHandle);
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