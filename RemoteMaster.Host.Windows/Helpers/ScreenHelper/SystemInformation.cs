﻿// Originally licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licensed the original code under the MIT license.
//
// Adapted by Vitaly Kuzyaev.
// This adapted version is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX;

namespace RemoteMaster.Host.Windows.Helpers.ScreenHelper;

public static class SystemInformation
{
    private static bool s_checkMultiMonitorSupport;
    private static bool s_multiMonitorSupport;

    public static Size PrimaryMonitorSize => GetSize(SM_CXSCREEN, SM_CYSCREEN);

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