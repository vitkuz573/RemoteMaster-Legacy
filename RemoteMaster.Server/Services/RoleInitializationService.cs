// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class RoleInitializationService(IServiceProvider serviceProvider) : IHostedService
{
    private readonly List<string> _roles = ["RootAdministrator", "Administrator", "Viewer"];

    private readonly Dictionary<string, List<Claim>> _roleClaims = new()
    {
        { "Administrator", AdministratorClaims },
        { "Viewer", ViewerClaims }
    };

    private static readonly List<Claim> AdministratorClaims =
    [
        new("Input", "MouseInput"),
        new("Input", "KeyboardInput"),
        new("Input", "ToggleInput"),
        new("Input", "BlockUserInput"),
        new("Screen", "SetFrameRate"),
        new("Screen", "SetImageQuality"),
        new("Screen", "Recording"),
        new("Screen", "ToggleDrawCursor"),
        new("Screen", "ChangeSelectedScreen"),
        new("Screen", "ToggleUseSkia"),
        new("Screen", "SetCodec"),
        new("Power", "RebootComputer"),
        new("Power", "ShutdownComputer"),
        new("Power", "WakeUpComputer"),
        new("Hardware", "SetMonitorState"),
        new("Security", "LockWorkStation"),
        new("Security", "LogOffUser"),
        new("HostManagement", "TerminateHost"),
        new("HostManagement", "Move"),
        new("HostManagement", "Remove"),
        new("HostManagement", "RenewCertificate"),
        new("Execution", "Scripts"),
        new("Execution", "ManagePsExecRules"),
        new("Execution", "OpenShell"),
        new("HostManagement", "Update"),
        new("Domain", "Membership"),
        new("Communication", "MessageBox"),
        new("TaskManagement", "OpenTaskManager"),
        new("FileManagement", "OpenFileManager"),
        new("FileManagement", "FileUpload"),
        new("HostInformation", "View"),
        new("Connection", "Control"),
        new("Connection", "View"),
        new("Service", "DisconnectClient"),
        new("Logs", "View")
    ];

    private static readonly List<Claim> ViewerClaims =
    [
        new("Screen", "ToggleDrawCursor"),
        new("Screen", "ChangeSelectedScreen")
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var applicationClaimsService = scope.ServiceProvider.GetRequiredService<IApplicationClaimsService>();

        foreach (var role in _roles)
        {
            await EnsureRoleExists(roleManager, role);
        }

        await EnsureClaimsExist(applicationClaimsService);
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

    private async Task EnsureClaimsExist(IApplicationClaimsService applicationClaimsService)
    {
        var claims = _roleClaims.SelectMany(rc => rc.Value).Distinct().ToList();

        foreach (var claim in claims)
        {
            var existingClaims = await applicationClaimsService.GetClaimsAsync(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);

            if (!existingClaims.Value.Any())
            {
                await applicationClaimsService.AddClaimAsync(new ApplicationClaim
                {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }
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
                    Log.Information("Successfully added claim {ClaimType} with value {ClaimValue} to role {RoleName}", claim.Type, claim.Value, roleName);
                }
                else
                {
                    Log.Error("Error adding claim {ClaimType} with value {ClaimValue} to role {RoleName}: {Errors}", claim.Type, claim.Value, roleName, string.Join(", ", result.Errors));
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
