// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class ShutdownService(IHostApplicationLifetime appLifetime) : IShutdownService
{
    public void SafeShutdown()
    {
        Log.Information("Initiating safe shutdown...");
        appLifetime.StopApplication();
    }

    public void ImmediateShutdown(int exitCode = 0)
    {
        Log.Information("Initiating immediate shutdown with exit code {ExitCode}...", exitCode);
        Environment.Exit(exitCode);
    }
}
