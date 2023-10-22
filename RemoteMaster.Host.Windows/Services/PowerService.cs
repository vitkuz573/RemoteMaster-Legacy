// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Abstractions;
using RemoteMaster.Host.Core.Abstractions;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Services;

public unsafe class PowerService : IPowerService
{
    private readonly ITokenPrivilegeService _tokenPrivilegeService;
    private readonly ILogger<PowerService> _logger;

    public PowerService(ITokenPrivilegeService tokenPrivilegeService, ILogger<PowerService> logger)
    {
        _tokenPrivilegeService = tokenPrivilegeService;
        _logger = logger;
    }

    public void Reboot(string message, uint timeout = 0, bool forceAppsClosed = true)
    {
        _logger.LogInformation("Attempting to reboot the system with message: {Message}, timeout: {Timeout}, forceAppsClosed: {ForceAppsClosed}", message, timeout, forceAppsClosed);
        _tokenPrivilegeService.AdjustPrivilege(SE_SHUTDOWN_NAME);

        fixed (char* pMessage = message)
        {
            var result = InitiateSystemShutdown(null, pMessage, timeout, forceAppsClosed, true);
            
            if (result == 0)
            {
                _logger.LogError("Failed to initiate system reboot.");
            }
            else
            {
                _logger.LogInformation("System reboot initiated successfully.");
            }
        }
    }

    public void Shutdown(string message, uint timeout = 0, bool forceAppsClosed = true)
    {
        _logger.LogInformation("Attempting to shutdown the system with message: {Message}, timeout: {Timeout}, forceAppsClosed: {ForceAppsClosed}", message, timeout, forceAppsClosed);
        _tokenPrivilegeService.AdjustPrivilege(SE_SHUTDOWN_NAME);

        fixed (char* pMessage = message)
        {
            var result = InitiateSystemShutdown(null, pMessage, timeout, forceAppsClosed, false);
            
            if (result == 0)
            {
                _logger.LogError("Failed to initiate system shutdown.");
            }
            else
            {
                _logger.LogInformation("System shutdown initiated successfully.");
            }
        }
    }
}
