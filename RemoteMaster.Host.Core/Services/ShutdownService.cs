// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ShutdownService(IHostApplicationLifetime appLifetime, ILogger<ShutdownService> logger) : IShutdownService
{
    public void SafeShutdown()
    {
        logger.LogInformation("Initiating safe shutdown...");
        appLifetime.StopApplication();
    }

    public void ImmediateShutdown(int exitCode = 0)
    {
        logger.LogInformation("Initiating immediate shutdown with exit code {ExitCode}...", exitCode);
        Environment.Exit(exitCode);
    }
}
