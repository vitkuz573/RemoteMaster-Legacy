// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostMonitorService : IHostedService
{
    private readonly IHostInstanceService _hostService;
    private Timer _timer;

    public HostMonitorService(IHostInstanceService hostService)
    {
        _hostService = hostService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(MonitorHostInstance, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        return Task.CompletedTask;
    }

    private void MonitorHostInstance(object state)
    {
        if (!_hostService.IsRunning())
        {
            Log.Information("Host instance is not running. Starting it...");
            _hostService.Start();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();

        return Task.CompletedTask;
    }
}

