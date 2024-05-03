// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class DatabaseCleanerService : IHostedService
{
    private readonly ITokenService _tokenService;
    private readonly Timer _timer;

    public DatabaseCleanerService(ITokenService tokenService)
    {
        _tokenService = tokenService;
        _timer = new Timer(MonitorDatabase, null, Timeout.Infinite, 0);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Change(TimeSpan.Zero, TimeSpan.FromHours(1));

        return Task.CompletedTask;
    }

    private void MonitorDatabase(object? state)
    {
        _tokenService.CleanUpExpiredRefreshTokens();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
