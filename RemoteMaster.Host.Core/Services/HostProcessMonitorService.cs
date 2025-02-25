// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class HostProcessMonitorService : IHostedService
{
    private readonly IUserInstanceService _userInstanceService;
    private readonly ILogger<HostProcessMonitorService> _logger;

    private readonly Timer _timer;

    public HostProcessMonitorService(IUserInstanceService userInstanceService, ILogger<HostProcessMonitorService> logger)
    {
        _userInstanceService = userInstanceService;
        _logger = logger;

        _timer = new Timer(MonitorHostInstanceAsync, null, Timeout.Infinite, 0);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));

        return Task.CompletedTask;
    }

    private async void MonitorHostInstanceAsync(object? state)
    {
        if (await _userInstanceService.IsRunningAsync())
        {
            return;
        }

        _logger.LogInformation("Host instance is not running. Starting it...");

        await _userInstanceService.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _timer.DisposeAsync();
    }
}
