// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

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
