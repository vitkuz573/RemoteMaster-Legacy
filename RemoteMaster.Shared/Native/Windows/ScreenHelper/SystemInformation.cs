using System.Drawing;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Native.Windows.ScreenHelper;

public static class SystemInformation
{
    private static bool s_checkMultiMonitorSupport;
    private static bool s_multiMonitorSupport;

    public static Size PrimaryMonitorSize => GetSize(SYSTEM_METRICS_INDEX.SM_CXSCREEN, SYSTEM_METRICS_INDEX.SM_CYSCREEN);

    public static unsafe Rectangle WorkingArea
    {
        get
        {
            var rect = new RECT();
            SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETWORKAREA, 0, &rect, 0);

            return rect;
        }
    }

    internal static bool MultiMonitorSupport
    {
        get
        {
            if (!s_checkMultiMonitorSupport)
            {
                s_multiMonitorSupport = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CMONITORS) != 0;
                s_checkMultiMonitorSupport = true;
            }

            return s_multiMonitorSupport;
        }
    }

    public static Rectangle VirtualScreen
    {
        get
        {
            if (MultiMonitorSupport)
            {
                return new(GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_XVIRTUALSCREEN), GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_YVIRTUALSCREEN), GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN), GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN));
            }
            else
            {
                var size = PrimaryMonitorSize;

                return new(0, 0, size.Width, size.Height);
            }
        }
    }

    private static Size GetSize(SYSTEM_METRICS_INDEX x, SYSTEM_METRICS_INDEX y) => new(GetSystemMetrics(x), GetSystemMetrics(y));
}