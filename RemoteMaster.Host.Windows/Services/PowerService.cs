// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Dtos;
using Serilog;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class PowerService(ITokenPrivilegeService tokenPrivilegeService) : IPowerService
{
    public void Reboot(PowerActionRequest powerActionRequest)
    {
        ArgumentNullException.ThrowIfNull(powerActionRequest);

        Log.Information("Attempting to reboot the system with message: {Message}, timeout: {Timeout}, forceAppsClosed: {ForceAppsClosed}", powerActionRequest.Message, powerActionRequest.Timeout, powerActionRequest.ForceAppsClosed);

        if (!tokenPrivilegeService.AdjustPrivilege(SE_SHUTDOWN_NAME))
        {
            Log.Error("Failed to adjust privileges for system reboot.");
            return;
        }

        bool result;

        unsafe
        {
            fixed (char* pMessage = powerActionRequest.Message)
            {
                result = InitiateSystemShutdown(null, pMessage, powerActionRequest.Timeout, powerActionRequest.ForceAppsClosed, true);
            }
        }

        if (!result)
        {
            Log.Error("Failed to initiate system reboot.");
        }
        else
        {
            Log.Information("System reboot initiated successfully.");
        }
    }

    public void Shutdown(PowerActionRequest powerActionRequest)
    {
        ArgumentNullException.ThrowIfNull(powerActionRequest);

        Log.Information("Attempting to shutdown the system with message: {Message}, timeout: {Timeout}, forceAppsClosed: {ForceAppsClosed}", powerActionRequest.Message, powerActionRequest.Timeout, powerActionRequest.ForceAppsClosed);

        if (!tokenPrivilegeService.AdjustPrivilege(SE_SHUTDOWN_NAME))
        {
            Log.Error("Failed to adjust privileges for system shutdown.");
            return;
        }

        bool result;

        unsafe
        {
            fixed (char* pMessage = powerActionRequest.Message)
            {
                result = InitiateSystemShutdown(null, pMessage, powerActionRequest.Timeout, powerActionRequest.ForceAppsClosed, false);
            }
        }

        if (!result)
        {
            Log.Error("Failed to initiate system shutdown.");
        }
        else
        {
            Log.Information("System shutdown initiated successfully.");
        }
    }
}

