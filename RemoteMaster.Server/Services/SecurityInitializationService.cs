// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

namespace RemoteMaster.Server.Services;

public class SecurityInitializationService(ICertificateAuthorityService certificateAuthorityService, IJwtSecurityService jwtSecurityService, IServiceScopeFactory serviceScopeFactory, ILogger<SecurityInitializationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        certificateAuthorityService.EnsureCaCertificateExists();
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
                    logger.LogError($"Error creating service user: {error.Description}");
                }
            }
        }
        else
        {
            logger.LogInformation("Service user already exists.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
