// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Host.Services;

public class HostService : IHostService
{
    private readonly ILogger<HostService> _logger;

    private static string CurrentExecutablePath => Environment.ProcessPath;
    private const string InstanceArgument = "--user-instance";

    public HostService(ILogger<HostService> logger)
    {
        _logger = logger;
    }

    public bool IsRunning()
    {
        return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(CurrentExecutablePath))
                      .Any(p => p.StartInfo.Arguments.Contains(InstanceArgument));
    }

    public void Start()
    {
        try
        {
            var options = new ProcessStartOptions(CurrentExecutablePath, -1)
            {
                Arguments = InstanceArgument,
                ForceConsoleSession = true,
                DesktopName = "default",
                HiddenWindow = false,
                UseCurrentUserToken = false
            };

            using var _ = NativeProcess.Start(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new instance of the host");
        }
    }

    public void Stop()
    {
        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(CurrentExecutablePath)))
        {
            if (process.StartInfo.Arguments.Contains(InstanceArgument))
            {
                process.Kill();
            }
        }
    }
}
