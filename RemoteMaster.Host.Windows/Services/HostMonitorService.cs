// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostMonitorService : IHostedService
{
    private readonly IHostInstanceService _hostInstanceService;
    private Timer _timer;

    public HostMonitorService(IHostInstanceService hostInstanceService)
    {
        _hostInstanceService = hostInstanceService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(MonitorHostInstance, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        return Task.CompletedTask;
    }

    private void MonitorHostInstance(object state)
    {
        if (!_hostInstanceService.IsRunning)
        {
            Log.Information("Host instance is not running. Starting it...");
            _hostInstanceService.Start();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();

        return Task.CompletedTask;
    }
}

