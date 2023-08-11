// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;

namespace RemoteMaster.Server.Core.Services;

public class ShutdownService : IShutdownService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<ShutdownService> _logger;

    public ShutdownService(IHostApplicationLifetime appLifetime, ILogger<ShutdownService> logger)
    {
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public void SafeShutdown()
    {
        _logger.LogInformation("Initiating safe shutdown...");
        _appLifetime.StopApplication();
    }

    public void ImmediateShutdown(int exitCode = 0)
    {
        _logger.LogInformation("Initiating immediate shutdown...");
        Environment.Exit(exitCode);
    }
}
