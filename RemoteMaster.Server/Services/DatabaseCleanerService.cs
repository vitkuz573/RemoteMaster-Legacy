// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Services;

public class DatabaseCleanerService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Timer _timer;

    public DatabaseCleanerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _timer = new Timer(MonitorDatabase, null, Timeout.Infinite, 0);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Change(TimeSpan.Zero, TimeSpan.FromHours(1));

        return Task.CompletedTask;
    }

    private async void MonitorDatabase(object? state)
    {
        using var scope = _serviceProvider.CreateScope();

        using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        try
        {
            await context.Database.OpenConnectionAsync();
            await tokenService.CleanUpExpiredRefreshTokens();
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();

        return Task.CompletedTask;
    }
}
