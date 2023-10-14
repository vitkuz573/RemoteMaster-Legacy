// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Helpers;

public class HardwareService : IHardwareService
{
    public void SetMonitorState(MonitorState state)
    {
        SendMessage(HWND.HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, (int)state);
    }
}
