// Copyright © 2023 Vitaly Kuzyaев. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace RemoteMaster.Server.Services;

public class RoleInitializationService(IServiceProvider serviceProvider) : IHostedService
{
    private readonly List<string> _roles = ["RootAdministrator", "Administrator", "Viewer"];

    private readonly Dictionary<string, List<Claim>> _roleClaims = new()
    {
        { "Administrator", AdminClaims },
        { "Viewer", ViewerClaims }
    };

    private static readonly List<Claim> AdminClaims =
    [
        new Claim("Mouse", "Input"),
        new Claim("Keyboard", "Input"),
        new Claim("Screen", "ToggleDrawCursor"),
        new Claim("Screen", "ChangeSelectedScreen"),
        new Claim("Input", "ToggleInput"),
        new Claim("Input", "BlockUserInput"),
        new Claim("Screen", "SetImageQuality"),
        new Claim("Power", "RebootComputer"),
        new Claim("Power", "ShutdownComputer"),
        new Claim("Hardware", "SetMonitorState"),
        new Claim("Script", "Execute"),
        new Claim("Security", "LockWorkStation"),
        new Claim("Security", "LogOffUser"),
        new Claim("HostManagement", "TerminateHost"),
        new Claim("HostManagement", "MoveHost"),
        new Claim("HostManagement", "RenewCertificate")
    ];

    private static readonly List<Claim> ViewerClaims =
    [
        new Claim("Screen", "ToggleDrawCursor"),
        new Claim("Screen", "ChangeSelectedScreen")
    ];

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
        foreach (var (roleName, claims) in _roleClaims)
        {
            var roleIdentity = await roleManager.FindByNameAsync(roleName);
            var existingClaims = await roleManager.GetClaimsAsync(roleIdentity);

            foreach (var claim in claims.Where(claim => !existingClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value)))
            {
                var result = await roleManager.AddClaimAsync(roleIdentity, claim);

                if (result.Succeeded)
                {
                    Log.Information("Successfully added claim {ClaimType} to role {RoleName}", claim.Type, roleName);
                }
                else
                {
                    Log.Error("Error adding claim {ClaimType} to role {RoleName}: {Errors}", claim.Type, roleName, string.Join(", ", result.Errors));
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
