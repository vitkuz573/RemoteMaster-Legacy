// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Services;

public class MigrationService(IServiceScopeFactory serviceScopeFactory, ILogger<MigrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var dbContexts = new List<DbContext>
        {
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            scope.ServiceProvider.GetRequiredService<CertificateDbContext>(),
            scope.ServiceProvider.GetRequiredService<TelegramBotDbContext>()
        };

        try
        {
            foreach (var dbContext in dbContexts)
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying the database migrations.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
