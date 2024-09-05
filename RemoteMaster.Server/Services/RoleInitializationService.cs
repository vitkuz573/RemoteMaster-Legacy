// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationClaimAggregate;
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

    private static readonly List<ApplicationClaim> AllClaims =
    [
        new ApplicationClaim("Input", "MouseInput", "Allow mouse input control"),
        new ApplicationClaim("Input", "KeyboardInput", "Allow keyboard input control"),
        new ApplicationClaim("Input", "ToggleInput", "Toggle input control"),
        new ApplicationClaim("Input", "BlockUserInput", "Block user input"),
        new ApplicationClaim("Screen", "SetFrameRate", "Set screen frame rate"),
        new ApplicationClaim("Screen", "SetImageQuality", "Set screen image quality"),
        new ApplicationClaim("Screen", "Recording", "Screen recording"),
        new ApplicationClaim("Screen", "ToggleDrawCursor", "Toggle drawing cursor"),
        new ApplicationClaim("Screen", "ChangeSelectedScreen", "Change selected screen"),
        new ApplicationClaim("Screen", "SetCodec", "Set screen codec"),
        new ApplicationClaim("Power", "RebootComputer", "Reboot the computer"),
        new ApplicationClaim("Power", "ShutdownComputer", "Shutdown the computer"),
        new ApplicationClaim("Power", "WakeUpComputer", "Wake up the computer"),
        new ApplicationClaim("Hardware", "SetMonitorState", "Set monitor state"),
        new ApplicationClaim("Security", "LockWorkStation", "Lock the workstation"),
        new ApplicationClaim("Security", "LogOffUser", "Log off the user"),
        new ApplicationClaim("HostManagement", "TerminateHost", "Terminate the host"),
        new ApplicationClaim("HostManagement", "Move", "Move the host"),
        new ApplicationClaim("HostManagement", "Remove", "Remove the host"),
        new ApplicationClaim("HostManagement", "RenewCertificate", "Renew the certificate"),
        new ApplicationClaim("Execution", "Scripts", "Execute scripts"),
        new ApplicationClaim("Execution", "ManagePsExecRules", "Manage PsExec rules"),
        new ApplicationClaim("Execution", "OpenShell", "Open shell"),
        new ApplicationClaim("HostManagement", "Update", "Update the host"),
        new ApplicationClaim("Domain", "Membership", "Manage domain membership"),
        new ApplicationClaim("Communication", "MessageBox", "Show message box"),
        new ApplicationClaim("Tasks", "Manager", "Open task manager"),
        new ApplicationClaim("Files", "Manager", "Open file manager"),
        new ApplicationClaim("Files", "Upload", "Upload files"),
        new ApplicationClaim("HostInformation", "View", "View host information"),
        new ApplicationClaim("Connect", "Control", "Control connection"),
        new ApplicationClaim("Connect", "View", "View connection"),
        new ApplicationClaim("Service", "DisconnectClient", "Disconnect any client"),
        new ApplicationClaim("Logs", "Manager", "Open logs manager")
    ];

    private static readonly List<Claim> AdministratorClaims =
    [
        new Claim("Input", "MouseInput"),
        new Claim("Input", "KeyboardInput"),
        new Claim("Input", "ToggleInput"),
        new Claim("Input", "BlockUserInput"),
        new Claim("Screen", "SetFrameRate"),
        new Claim("Screen", "SetImageQuality"),
        new Claim("Screen", "Recording"),
        new Claim("Screen", "ToggleDrawCursor"),
        new Claim("Screen", "ChangeSelectedScreen"),
        new Claim("Screen", "SetCodec"),
        new Claim("Power", "RebootComputer"),
        new Claim("Power", "ShutdownComputer"),
        new Claim("Power", "WakeUpComputer"),
        new Claim("Hardware", "SetMonitorState"),
        new Claim("Security", "LockWorkStation"),
        new Claim("Security", "LogOffUser"),
        new Claim("HostManagement", "TerminateHost"),
        new Claim("HostManagement", "Move"),
        new Claim("HostManagement", "Remove"),
        new Claim("HostManagement", "RenewCertificate"),
        new Claim("Execution", "Scripts"),
        new Claim("Execution", "ManagePsExecRules"),
        new Claim("Execution", "OpenShell"),
        new Claim("HostManagement", "Update"),
        new Claim("Domain", "Membership"),
        new Claim("Communication", "MessageBox"),
        new Claim("Tasks", "Manager"),
        new Claim("Files", "Manager"),
        new Claim("Files", "Upload"),
        new Claim("HostInformation", "View"),
        new Claim("Connect", "Control"),
        new Claim("Connect", "View"),
        new Claim("Service", "DisconnectClient"),
        new Claim("Logs", "Manager"),
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
        var applicationClaimRepository = scope.ServiceProvider.GetRequiredService<IApplicationClaimRepository>();

        foreach (var role in _roles)
        {
            await EnsureRoleExists(roleManager, role);
        }

        await EnsureClaimsExist(applicationClaimRepository);
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

    private async Task EnsureClaimsExist(IApplicationClaimRepository applicationClaimRepository)
    {
        var claims = _roleClaims.SelectMany(rc => rc.Value).Distinct().ToList();

        foreach (var claim in claims)
        {
            var existingClaims = await applicationClaimRepository.FindAsync(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);

            if (existingClaims.Any())
            {
                continue;
            }

            var matchingClaim = AllClaims.FirstOrDefault(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value) ?? throw new InvalidOperationException($"Claim '{claim.Type}:{claim.Value}' is assigned to a role but does not exist in the central repository.");

            await applicationClaimRepository.AddAsync(new ApplicationClaim(claim.Type, claim.Value, matchingClaim.Description));

            await applicationClaimRepository.SaveChangesAsync();
        }
    }

    private async Task AssignClaimsToRoles(RoleManager<IdentityRole> roleManager)
    {
        foreach (var (roleName, claims) in _roleClaims)
        {
            var roleIdentity = await roleManager.FindByNameAsync(roleName);

            if (roleIdentity == null)
            {
                Log.Error("Role {RoleName} not found, skipping claim assignment.", roleName);

                continue;
            }

            var existingClaims = await roleManager.GetClaimsAsync(roleIdentity);

            foreach (var claim in claims.Where(cl => !existingClaims.Any(c => c.Type == cl.Type && c.Value == cl.Value)))
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
