// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using Serilog;

namespace RemoteMaster.Server.Services;

public class SecurityInitializationService(ICaCertificateService caCertificateService, IJwtSecurityService jwtSecurityService, IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        caCertificateService.EnsureCaCertificateExists();
        await jwtSecurityService.EnsureKeysExistAsync();

        await EnsureServiceUserExistsAsync();
    }

    private async Task EnsureServiceUserExistsAsync()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string serviceUserId = "service_user";

        var existingUser = await userManager.FindByIdAsync(serviceUserId);

        if (existingUser == null)
        {
            var serviceUser = new ApplicationUser
            {
                Id = serviceUserId,
                UserName = "service",
                SecurityStamp = Guid.NewGuid().ToString("D")
            };

            var result = await userManager.CreateAsync(serviceUser);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(serviceUser, "ServiceUser");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    Log.Error($"Error creating service user: {error.Description}");
                }
            }
        }
        else
        {
            Log.Information("Service user already exists.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
