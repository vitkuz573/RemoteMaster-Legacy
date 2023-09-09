// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Native.Windows;

[SupportedOSPlatform("windows6.0.6000")]
public static class HardwareHelper
{
    public enum MonitorState
    {
        On = -1,
        Standby = 1,
        Off = 2
    }

    public static void SetMonitorState(MonitorState state)
    {
        SendMessage((HWND)0xFFFF, WM_SYSCOMMAND, SC_MONITORPOWER, (int)state);
    }
}
