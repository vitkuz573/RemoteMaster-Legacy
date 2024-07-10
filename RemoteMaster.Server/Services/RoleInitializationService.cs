// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace RemoteMaster.Server.Services;

public class RoleInitializationService(IServiceProvider serviceProvider) : IHostedService
{
    private readonly List<string> _roles = ["RootAdministrator", "Administrator", "Viewer"];

    private readonly Dictionary<string, List<string>> _roleClaims = new()
    {
        { "Administrator", new List<string>
            {
                "MouseInput", "KeyboardInput", "ToggleCursorTracking", "SwitchScreen", "ToggleInput", "ToggleUserInput",
                "ChangeImageQuality", "TerminateHost", "RebootComputer", "ShutdownComputer",
                "ChangeMonitorState", "ExecuteScript", "LockWorkStation", "LogOffUser", "Move", "RenewCertificate"
            }
        },
        { "Viewer", new List<string>
            {
                "ToggleCursorTracking", "SwitchScreen"
            }
        }
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in _roles)
        {
            await EnsureRoleExists(roleManager, role);
        }

        await AssignClaimsToRoles(roleManager);
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

    private async Task AssignClaimsToRoles(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleClaims in _roleClaims)
        {
            var roleName = roleClaims.Key;
            var claims = roleClaims.Value;

            var roleIdentity = await roleManager.FindByNameAsync(roleName);
            var existingClaims = await roleManager.GetClaimsAsync(roleIdentity);

            foreach (var claim in claims)
            {
                if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == claim))
                {
                    var result = await roleManager.AddClaimAsync(roleIdentity, new Claim("Permission", claim));
                    
                    if (result.Succeeded)
                    {
                        Log.Information("Successfully added claim {ClaimType} to role {RoleName}", claim, roleName);
                    }
                    else
                    {
                        Log.Error("Error adding claim {ClaimType} to role {RoleName}: {Errors}", claim, roleName, string.Join(", ", result.Errors));
                    }
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
