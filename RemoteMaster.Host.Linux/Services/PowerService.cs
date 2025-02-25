// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Linux.Services;

public class PowerService(IProcessWrapperFactory processWrapperFactory, ILogger<PowerService> logger) : IPowerService
{
    public async Task ShutdownAsync(PowerActionRequest powerActionRequest)
    {
        ArgumentNullException.ThrowIfNull(powerActionRequest);

        logger.LogInformation("Attempting to shutdown the system with message: {Message}, timeout: {Timeout} seconds, forceAppsClosed: {ForceAppsClosed}", powerActionRequest.Message, powerActionRequest.Timeout, powerActionRequest.ForceAppsClosed);
        
        await ExecuteShutdownCommandAsync(false, powerActionRequest);
    }

    public async Task RebootAsync(PowerActionRequest powerActionRequest)
    {
        ArgumentNullException.ThrowIfNull(powerActionRequest);

        logger.LogInformation("Attempting to reboot the system with message: {Message}, timeout: {Timeout} seconds, forceAppsClosed: {ForceAppsClosed}", powerActionRequest.Message, powerActionRequest.Timeout, powerActionRequest.ForceAppsClosed);
        
        await ExecuteShutdownCommandAsync(true, powerActionRequest);
    }

    private async Task ExecuteShutdownCommandAsync(bool isReboot, PowerActionRequest powerActionRequest)
    {
        var timeArg = powerActionRequest.Timeout <= 0 ? "now" : $"+{Math.Ceiling(powerActionRequest.Timeout / 60.0)}";
        var message = string.IsNullOrWhiteSpace(powerActionRequest.Message) ? "" : powerActionRequest.Message;
        var modeFlag = isReboot ? "-r" : "-h";
        var arguments = $"{modeFlag} {timeArg} \"{message}\"";

        logger.LogInformation("Executing shutdown command: shutdown {Arguments}", arguments);

        try
        {
            var process = processWrapperFactory.Create();

            process.Start(new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var errorOutput = await process.StandardError.ReadToEndAsync();
                
                logger.LogError("Shutdown command failed with exit code {ExitCode}: {ErrorOutput}", process.ExitCode, errorOutput);
            }
            else
            {
                logger.LogInformation("Shutdown command executed successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while executing shutdown command.");
        }
    }
}
