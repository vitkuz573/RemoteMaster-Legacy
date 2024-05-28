// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using Serilog;

namespace RemoteMaster.Server.Services;

public class RoleInitializationService(IServiceProvider serviceProvider) : IHostedService
{
    private readonly List<string> _roles = ["RootAdministrator", "Administrator", "Viewer"];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in _roles)
        {
            await EnsureRoleExists(roleManager, role);
        }
    }

    private static async Task EnsureRoleExists(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));

            if (result.Succeeded)
            {
                Log.Information("Successfully created role {RoleName}", roleName);
            }
            else
            {
                Log.Error("Error creating role {RoleName}: {Errors}", roleName, string.Join(", ", result.Errors));
            }
        }
        else
        {
            Log.Information("Role {RoleName} already exists.", roleName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
