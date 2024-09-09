// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.


using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
using Serilog;

namespace RemoteMaster.Server.Services;

public class MigrationService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var certificateDbContext = scope.ServiceProvider.GetRequiredService<CertificateDbContext>();

        var dbContexts = new List<DbContext>
        {
            applicationDbContext,
            certificateDbContext
        };

        try
        {
            foreach (var dbContext in dbContexts)
            {
                dbContext.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying the database migrations.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
