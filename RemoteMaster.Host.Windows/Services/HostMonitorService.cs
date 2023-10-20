// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Services;

public class HostMonitorService : IHostedService
{
    private readonly IHostInstanceService _hostService;
    private readonly ILogger<HostMonitorService> _logger;
    private Timer _timer;

    public HostMonitorService(IHostInstanceService hostService, ILogger<HostMonitorService> logger)
    {
        _hostService = hostService;
        _logger = logger;
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
            _logger.LogInformation("Host instance is not running. Starting it...");
            _hostService.Start();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();

        return Task.CompletedTask;
    }
}

