// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public unsafe class PowerService(ITokenPrivilegeService tokenPrivilegeService) : IPowerService
{
    public void Reboot(string message, uint timeout = 0, bool forceAppsClosed = true)
    {
        Log.Information("Attempting to reboot the system with message: {Message}, timeout: {Timeout}, forceAppsClosed: {ForceAppsClosed}", message, timeout, forceAppsClosed);
        tokenPrivilegeService.AdjustPrivilege(SE_SHUTDOWN_NAME);

        fixed (char* pMessage = message)
        {
            var result = InitiateSystemShutdown(null, pMessage, timeout, forceAppsClosed, true);

            if (result == 0)
            {
                Log.Error("Failed to initiate system reboot.");
            }
            else
            {
                Log.Information("System reboot initiated successfully.");
            }
        }
    }

    public void Shutdown(string message, uint timeout = 0, bool forceAppsClosed = true)
    {
        Log.Information("Attempting to shutdown the system with message: {Message}, timeout: {Timeout}, forceAppsClosed: {ForceAppsClosed}", message, timeout, forceAppsClosed);
        tokenPrivilegeService.AdjustPrivilege(SE_SHUTDOWN_NAME);

        fixed (char* pMessage = message)
        {
            var result = InitiateSystemShutdown(null, pMessage, timeout, forceAppsClosed, false);

            if (result == 0)
            {
                Log.Error("Failed to initiate system shutdown.");
            }
            else
            {
                Log.Information("System shutdown initiated successfully.");
            }
        }
    }
}
