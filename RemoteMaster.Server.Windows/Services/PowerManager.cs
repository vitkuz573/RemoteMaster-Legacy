// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Shared.Native.Windows;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public unsafe class PowerManager : IPowerManager
{
    public void Reboot(string message, uint timeout = 0, bool forceAppsClosed = true)
    {
        TokenPrivilegeHelper.AdjustTokenPrivilege(SE_SHUTDOWN_NAME);

        fixed (char* pMessage = message)
        {
            InitiateSystemShutdown(null, pMessage, timeout, forceAppsClosed, true);
        }
    }

    public void Shutdown(string message, uint timeout = 0, bool forceAppsClosed = true)
    {
        TokenPrivilegeHelper.AdjustTokenPrivilege(SE_SHUTDOWN_NAME);

        fixed (char* pMessage = message)
        {
            InitiateSystemShutdown(null, pMessage, timeout, forceAppsClosed, false);
        }
    }
}
