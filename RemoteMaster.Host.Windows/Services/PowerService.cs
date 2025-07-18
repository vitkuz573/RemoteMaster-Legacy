﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.DTOs;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class PowerService(ITokenPrivilegeService tokenPrivilegeService, ILogger<PowerService> logger) : IPowerService
{
    public Task RebootAsync(PowerActionRequest powerActionRequest)
    {
        ArgumentNullException.ThrowIfNull(powerActionRequest);

        logger.LogInformation("Attempting to reboot the system with message: {Message}, timeout: {Timeout}, forceAppsClosed: {ForceAppsClosed}", powerActionRequest.Message, powerActionRequest.Timeout, powerActionRequest.ForceAppsClosed);

        if (!tokenPrivilegeService.AdjustPrivilege(SE_SHUTDOWN_NAME))
        {
            logger.LogError("Failed to adjust privileges for system reboot.");

            return Task.CompletedTask;
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
            logger.LogError("Failed to initiate system reboot.");
        }
        else
        {
            logger.LogInformation("System reboot initiated successfully.");
        }

        return Task.CompletedTask;
    }

    public Task ShutdownAsync(PowerActionRequest powerActionRequest)
    {
        ArgumentNullException.ThrowIfNull(powerActionRequest);

        logger.LogInformation("Attempting to shutdown the system with message: {Message}, timeout: {Timeout}, forceAppsClosed: {ForceAppsClosed}", powerActionRequest.Message, powerActionRequest.Timeout, powerActionRequest.ForceAppsClosed);

        if (!tokenPrivilegeService.AdjustPrivilege(SE_SHUTDOWN_NAME))
        {
            logger.LogError("Failed to adjust privileges for system shutdown.");

            return Task.CompletedTask;
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
            logger.LogError("Failed to initiate system shutdown.");
        }
        else
        {
            logger.LogInformation("System shutdown initiated successfully.");
        }

        return Task.CompletedTask;
    }
}
