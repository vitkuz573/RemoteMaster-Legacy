// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Shared.Native.Windows;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class PowerManager : IPowerManager
{
    public void Reboot()
    {
        TokenPrivilegeHelper.AdjustTokenPrivilege(SE_SHUTDOWN_NAME);
        InitiateSystemShutdown(null, null, 0, true, true);
    }

    public void Shutdown()
    {
        TokenPrivilegeHelper.AdjustTokenPrivilege(SE_SHUTDOWN_NAME);
        InitiateSystemShutdown(null, null, 0, true, false);
    }
}
