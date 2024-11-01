// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationClaimAggregate;

namespace RemoteMaster.Server.Services;

public class RoleInitializationService(IServiceProvider serviceProvider, ILogger<RoleInitializationService> logger) : IHostedService
{
    private readonly List<string> _roles = ["RootAdministrator", "Administrator", "Viewer", "ServiceUser"];

    private readonly Dictionary<string, List<Claim>> _roleClaims = new()
    {
        { "Administrator", AdministratorClaims },
        { "Viewer", ViewerClaims },
        { "ServiceUser", ServiceUserClaims }
    };

    private static readonly List<ApplicationClaim> AllClaims =
    [
        new ApplicationClaim("Input", "MouseInput", "Mouse Input", "Allow mouse input control"),
        new ApplicationClaim("Input", "KeyboardInput", "Keyboard Input", "Allow keyboard input control"),
        new ApplicationClaim("Input", "ToggleInput", "Toggle Input", "Toggle input control"),
        new ApplicationClaim("Input", "ToggleClickIndicator", "Toggle Click Indicator", "Toggle click indicator"),
        new ApplicationClaim("Input", "BlockUserInput", "Block User Input", "Block user input"),
        new ApplicationClaim("Screen", "SetFrameRate", "Set Frame Rate", "Set screen frame rate"),
        new ApplicationClaim("Screen", "SetImageQuality", "Set Image Quality", "Set screen image quality"),
        new ApplicationClaim("Screen", "Recording", "Screen Recording", "Screen recording"),
        new ApplicationClaim("Screen", "ToggleDrawCursor", "Toggle Draw Cursor", "Toggle drawing cursor"),
        new ApplicationClaim("Screen", "ChangeSelectedScreen", "Change Selected Screen", "Change selected screen"),
        new ApplicationClaim("Screen", "SetCodec", "Set Codec", "Set screen codec"),
        new ApplicationClaim("Power", "RebootHost", "Reboot Host", "Reboot the host"),
        new ApplicationClaim("Power", "ShutdownHost", "Shutdown Host", "Shutdown the host"),
        new ApplicationClaim("Power", "WakeUpHost", "Wake Up Host", "Wake up the host"),
        new ApplicationClaim("Hardware", "SetMonitorState", "Set Monitor State", "Set monitor state"),
        new ApplicationClaim("Security", "LockWorkStation", "Lock Workstation", "Lock the workstation"),
        new ApplicationClaim("Security", "LogOffUser", "Log Off User", "Log off the user"),
        new ApplicationClaim("HostManagement", "TerminateHost", "Terminate Host", "Terminate the host"),
        new ApplicationClaim("HostManagement", "Move", "Move Host", "Move the host"),
        new ApplicationClaim("HostManagement", "Remove", "Remove Host", "Remove the host"),
        new ApplicationClaim("HostManagement", "RenewCertificate", "Renew Certificate", "Renew the certificate"),
        new ApplicationClaim("HostManagement", "Update", "Update Host", "Update the host"),
        new ApplicationClaim("HostManagement", "BootWim", "Boot WIM", "Boot PC from WIM file"),
        new ApplicationClaim("Execution", "Scripts", "Execute Scripts", "Execute scripts"),
        new ApplicationClaim("Execution", "ManagePsExecRules", "Manage PsExec Rules", "Manage PsExec rules"),
        new ApplicationClaim("Execution", "OpenShell", "Open Shell", "Open shell"),
        new ApplicationClaim("Domain", "Membership", "Manage Domain Membership", "Manage domain membership"),
        new ApplicationClaim("Communication", "MessageBox", "Show Message Box", "Show message box"),
        new ApplicationClaim("Communication", "Chat", "Open Chat", "Open chat"),
        new ApplicationClaim("Tasks", "Manager", "Task Manager", "Open task manager"),
        new ApplicationClaim("Devices", "Manager", "Device Manager", "Open device manager"),
        new ApplicationClaim("Files", "Manager", "File Manager", "Open file manager"),
        new ApplicationClaim("Files", "Upload", "Upload Files", "Upload files"),
        new ApplicationClaim("Registry", "Editor", "Registry Editor", "Open registry editor"),
        new ApplicationClaim("HostInformation", "View", "View Host Information", "View host information"),
        new ApplicationClaim("Connect", "Control", "Control Connection", "Control connection"),
        new ApplicationClaim("Connect", "View", "View Connection", "View connection"),
        new ApplicationClaim("Service", "DisconnectClient", "Disconnect Client", "Disconnect any client"),
        new ApplicationClaim("Logs", "Manager", "Logs Manager", "Open logs manager")
    ];

    private static readonly List<Claim> AdministratorClaims =
    [
        new Claim("Input", "MouseInput"),
        new Claim("Input", "KeyboardInput"),
        new Claim("Input", "ToggleInput"),
        new Claim("Input", "ToggleClickIndicator"),
        new Claim("Input", "BlockUserInput"),
        new Claim("Screen", "SetFrameRate"),
        new Claim("Screen", "SetImageQuality"),
        new Claim("Screen", "Recording"),
        new Claim("Screen", "ToggleDrawCursor"),
        new Claim("Screen", "ChangeSelectedScreen"),
        new Claim("Screen", "SetCodec"),
        new Claim("Power", "RebootHost"),
        new Claim("Power", "ShutdownHost"),
        new Claim("Power", "WakeUpHost"),
        new Claim("Hardware", "SetMonitorState"),
        new Claim("Security", "LockWorkStation"),
        new Claim("Security", "LogOffUser"),
        new Claim("HostManagement", "TerminateHost"),
        new Claim("HostManagement", "Move"),
        new Claim("HostManagement", "Remove"),
        new Claim("HostManagement", "RenewCertificate"),
        new Claim("HostManagement", "Update"),
        new Claim("HostManagement", "BootWim"),
        new Claim("Execution", "Scripts"),
        new Claim("Execution", "ManagePsExecRules"),
        new Claim("Execution", "OpenShell"),
        new Claim("Domain", "Membership"),
        new Claim("Communication", "MessageBox"),
        new Claim("Communication", "Chat"),
        new Claim("Tasks", "Manager"),
        new Claim("Devices", "Manager"),
        new Claim("Files", "Manager"),
        new Claim("Files", "Upload"),
        new Claim("Registry", "Editor"),
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

    private static readonly List<Claim> ServiceUserClaims =
    [
        new Claim("HostManagement", "RenewCertificate")
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var applicationUnitOfWork = scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>();

        foreach (var role in _roles)
        {
            await EnsureRoleExists(roleManager, role);
        }

        await EnsureClaimsExist(applicationUnitOfWork);
        await AssignClaimsToRoles(roleManager);
    }

    private async Task EnsureRoleExists(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));

            if (result.Succeeded)
            {
                logger.LogInformation("Successfully created role {RoleName}", roleName);
            }
            else
            {
                logger.LogError("Error creating role {RoleName}: {Errors}", roleName, string.Join(", ", result.Errors));
            }
        }
        else
        {
            logger.LogInformation("Role {RoleName} already exists.", roleName);
        }
    }

    private async Task EnsureClaimsExist(IApplicationUnitOfWork applicationUnitOfWork)
    {
        var claims = _roleClaims.SelectMany(rc => rc.Value).Distinct().ToList();

        foreach (var claim in claims)
        {
            var existingClaims = await applicationUnitOfWork.ApplicationClaims.FindAsync(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);

            if (existingClaims.Any())
            {
                continue;
            }

            var matchingClaim = AllClaims.FirstOrDefault(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value) ?? throw new InvalidOperationException($"Claim '{claim.Type}:{claim.Value}' is assigned to a role but does not exist in the predefined claim list.");

            await applicationUnitOfWork.ApplicationClaims.AddAsync(new ApplicationClaim(claim.Type, claim.Value, matchingClaim.DisplayName, matchingClaim.Description));

            await applicationUnitOfWork.CommitAsync();
        }
    }

    private async Task AssignClaimsToRoles(RoleManager<IdentityRole> roleManager)
    {
        foreach (var (roleName, claims) in _roleClaims)
        {
            var roleIdentity = await roleManager.FindByNameAsync(roleName);

            if (roleIdentity == null)
            {
                logger.LogError("Role {RoleName} not found, skipping claim assignment.", roleName);

                continue;
            }

            var existingClaims = await roleManager.GetClaimsAsync(roleIdentity);

            foreach (var claim in claims.Where(cl => !existingClaims.Any(c => c.Type == cl.Type && c.Value == cl.Value)))
            {
                var result = await roleManager.AddClaimAsync(roleIdentity, claim);

                if (result.Succeeded)
                {
                    logger.LogInformation("Successfully added claim {ClaimType} with value {ClaimValue} to role {RoleName}", claim.Type, claim.Value, roleName);
                }
                else
                {
                    logger.LogError("Error adding claim {ClaimType} with value {ClaimValue} to role {RoleName}: {Errors}", claim.Type, claim.Value, roleName, string.Join(", ", result.Errors));
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
