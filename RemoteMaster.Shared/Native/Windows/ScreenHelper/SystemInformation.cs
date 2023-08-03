// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX;
using static Windows.Win32.UI.WindowsAndMessaging.SYSTEM_PARAMETERS_INFO_ACTION;

namespace RemoteMaster.Shared.Native.Windows.ScreenHelper;

[SupportedOSPlatform("windows6.0.6000")]
public static class SystemInformation
{
    private static bool s_checkMultiMonitorSupport;
    private static bool s_multiMonitorSupport;

    public static Size PrimaryMonitorSize => GetSize(SM_CXSCREEN, SM_CYSCREEN);

    public static unsafe Rectangle WorkingArea
    {
        get
        {
            var rect = default(RECT);
            PInvoke.SystemParametersInfo(SPI_GETWORKAREA, 0, &rect, 0);
            return rect;
        }
    }

    internal static bool MultiMonitorSupport
    {
        get
        {
            if (!s_checkMultiMonitorSupport)
            {
                s_multiMonitorSupport = PInvoke.GetSystemMetrics(SM_CMONITORS) != 0;
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
                return new(PInvoke.GetSystemMetrics(SM_XVIRTUALSCREEN),
                           PInvoke.GetSystemMetrics(SM_YVIRTUALSCREEN),
                           PInvoke.GetSystemMetrics(SM_CXVIRTUALSCREEN),
                           PInvoke.GetSystemMetrics(SM_CYVIRTUALSCREEN));
            }

            Size size = PrimaryMonitorSize;
            return new Rectangle(0, 0, size.Width, size.Height);
        }
    }

    private static Size GetSize(SYSTEM_METRICS_INDEX x, SYSTEM_METRICS_INDEX y) => new(PInvoke.GetSystemMetrics(x), PInvoke.GetSystemMetrics(y));
}