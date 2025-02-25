// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class DatabaseCleanerService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Timer _timer;

    public DatabaseCleanerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _timer = new Timer(MonitorDatabaseAsync, null, Timeout.Infinite, 0);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Change(TimeSpan.Zero, TimeSpan.FromHours(1));

        return Task.CompletedTask;
    }

    private async void MonitorDatabaseAsync(object? state)
    {
        using var scope = _serviceProvider.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        await tokenService.CleanUpExpiredRefreshTokensAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _timer.DisposeAsync();
    }
}
