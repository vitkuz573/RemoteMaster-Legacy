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
        new ApplicationClaim("DeviceManagement", "View", "View Devices", "Allow viewing devices"),
        new ApplicationClaim("DeviceManagement", "Disable", "Disable Devices", "Allow disabling devices"),
        new ApplicationClaim("DeviceManagement", "Enable", "Enable Devices", "Allow enabling devices"),
        new ApplicationClaim("DeviceManagement", "UpdateDriver", "Update Device Drivers", "Allow updating device drivers"),
        new ApplicationClaim("FileManagement", "Upload", "Upload Files", "Allow uploading files"),
        new ApplicationClaim("FileManagement", "Download", "Download Files", "Allow downloading files"),
        new ApplicationClaim("FileManagement", "View", "View Files", "Allow viewing files"),
        new ApplicationClaim("FileManagement", "GetDrives", "Get Drives", "Allow viewing available drives"),
        new ApplicationClaim("LogManagement", "ViewFiles", "View Log Files", "Allow viewing log files"),
        new ApplicationClaim("LogManagement", "ViewContent", "View Log Content", "Allow viewing log content"),
        new ApplicationClaim("LogManagement", "Filter", "Filter Logs", "Allow filtering logs"),
        new ApplicationClaim("LogManagement", "Delete", "Delete Logs", "Allow deleting logs"),
        new ApplicationClaim("ScreenRecording", "Start", "Start Screen Recording", "Allow starting screen recording"),
        new ApplicationClaim("ScreenRecording", "Stop", "Stop Screen Recording", "Allow stopping screen recording"),
        new ApplicationClaim("TaskManagement", "View", "View Processes", "Allow viewing running processes"),
        new ApplicationClaim("TaskManagement", "Kill", "Kill Processes", "Allow killing processes"),
        new ApplicationClaim("TaskManagement", "Start", "Start Processes", "Allow starting new processes"),
        new ApplicationClaim("UpdaterManagement", "Start", "Start Updater", "Allow starting updater"),
        new ApplicationClaim("DomainManagement", "Join", "Join Domain", "Allow joining a domain"),
        new ApplicationClaim("DomainManagement", "Unjoin", "Unjoin Domain", "Allow unjoining from a domain"),
        new ApplicationClaim("ChatManagement", "Send", "Send Messages", "Allow sending messages in chat"),
        new ApplicationClaim("ChatManagement", "Delete", "Delete Messages", "Allow deleting messages in chat"),
        new ApplicationClaim("CertificateManagement", "Renew", "Renew Certificate", "Allow renewing certificates"),
        new ApplicationClaim("CertificateManagement", "GetSerialNumber", "Get Certificate Serial Number", "Allow fetching certificate serial numbers"),
        new ApplicationClaim("CertificateManagement", "ViewTasks", "View Certificate Tasks", "Allow access to open certificate-related tasks"),
        new ApplicationClaim("CertificateManagement", "PublishCRL", "Publish CRL", "Allow publishing Certificate Revocation Lists"),
        new ApplicationClaim("RegistryManagement", "GetRootKeys", "Get Root Keys", "Allow fetching registry root keys"),
        new ApplicationClaim("RegistryManagement", "GetValue", "Get Registry Values", "Allow fetching registry values"),
        new ApplicationClaim("RegistryManagement", "SetValue", "Set Registry Values", "Allow setting registry values"),
        new ApplicationClaim("RegistryManagement", "GetSubKeys", "Get Sub Keys", "Allow fetching registry subkeys"),
        new ApplicationClaim("RegistryManagement", "GetAllValues", "Get All Values", "Allow fetching all registry values"),
        new ApplicationClaim("RegistryManagement", "ExportBranch", "Export Registry Branch", "Allow exporting registry branches"),
        new ApplicationClaim("Service", "DisconnectClient", "Disconnect Client", "Allow disconnecting clients"),
        new ApplicationClaim("Input", "Handle", "Handle Input", "Allow handling input"),
        new ApplicationClaim("Input", "Toggle", "Toggle Input", "Allow toggling input"),
        new ApplicationClaim("Input", "Block", "Block Input", "Allow blocking input"),
        new ApplicationClaim("Screen", "Change", "Change Screen", "Allow changing screen selection"),
        new ApplicationClaim("Screen", "SetFrameRate", "Set Frame Rate", "Allow setting screen frame rate"),
        new ApplicationClaim("Screen", "SetQuality", "Set Image Quality", "Allow setting screen image quality"),
        new ApplicationClaim("Screen", "SetCodec", "Set Codec", "Allow setting screen codec"),
        new ApplicationClaim("Screen", "ToggleCursor", "Toggle Cursor Drawing", "Allow toggling cursor drawing"),
        new ApplicationClaim("Hardware", "SetMonitorState", "Set Monitor State", "Allow setting monitor state"),
        new ApplicationClaim("Execution", "Scripts", "Execute Scripts", "Allow executing scripts"),
        new ApplicationClaim("Execution", "OpenShell", "Open Shell", "Allow opening a shell"),
        new ApplicationClaim("Security", "LockWorkStation", "Lock Workstation", "Allow locking workstation"),
        new ApplicationClaim("Security", "LogOffUser", "Log Off User", "Allow logging off user"),
        new ApplicationClaim("Security", "SendCtrlAltDel", "Send Ctrl+Alt+Del", "Allow sending Ctrl+Alt+Del command"),
        new ApplicationClaim("HostManagement", "Terminate", "Terminate Host", "Allow terminating host"),
        new ApplicationClaim("HostManagement", "View", "View Hosts", "Allow viewing host details"),
        new ApplicationClaim("HostManagement", "Move", "Move Host", "Allow moving host"),
        new ApplicationClaim("HostManagement", "Remove", "Remove Hosts", "Allow removing hosts from the system"),
        new ApplicationClaim("Power", "Reboot", "Reboot Host", "Allow rebooting host"),
        new ApplicationClaim("Power", "Shutdown", "Shutdown Host", "Allow shutting down host"),
        new ApplicationClaim("Power", "WakeUp", "Wake Up Host", "Allow waking up the host"),
        new ApplicationClaim("Connect", "Control", "Control Connection", "Allow connecting for control"),
        new ApplicationClaim("Connect", "View", "View Connection", "Allow connecting for viewing"),
    ];

    private static readonly List<Claim> AdministratorClaims =
    [
        new Claim("DeviceManagement", "View"),
        new Claim("DeviceManagement", "Disable"),
        new Claim("DeviceManagement", "Enable"),
        new Claim("DeviceManagement", "UpdateDriver"),
        new Claim("FileManagement", "Upload"),
        new Claim("FileManagement", "Download"),
        new Claim("FileManagement", "View"),
        new Claim("FileManagement", "GetDrives"),
        new Claim("LogManagement", "ViewFiles"),
        new Claim("LogManagement", "ViewContent"),
        new Claim("LogManagement", "Filter"),
        new Claim("LogManagement", "Delete"),
        new Claim("ScreenRecording", "Start"),
        new Claim("ScreenRecording", "Stop"),
        new Claim("TaskManagement", "View"),
        new Claim("TaskManagement", "Kill"),
        new Claim("TaskManagement", "Start"),
        new Claim("UpdaterManagement", "Start"),
        new Claim("DomainManagement", "Join"),
        new Claim("DomainManagement", "Unjoin"),
        new Claim("ChatManagement", "Send"),
        new Claim("ChatManagement", "Delete"),
        new Claim("CertificateManagement", "Renew"),
        new Claim("CertificateManagement", "GetSerialNumber"),
        new Claim("CertificateManagement", "ViewTasks"),
        new Claim("CertificateManagement", "PublishCRL"),
        new Claim("RegistryManagement", "GetRootKeys"),
        new Claim("RegistryManagement", "GetValue"),
        new Claim("RegistryManagement", "SetValue"),
        new Claim("RegistryManagement", "GetSubKeys"),
        new Claim("RegistryManagement", "GetAllValues"),
        new Claim("RegistryManagement", "ExportBranch"),
        new Claim("Service", "DisconnectClient"),
        new Claim("Input", "Handle"),
        new Claim("Input", "Toggle"),
        new Claim("Input", "Block"),
        new Claim("Screen", "Change"),
        new Claim("Screen", "SetFrameRate"),
        new Claim("Screen", "SetQuality"),
        new Claim("Screen", "SetCodec"),
        new Claim("Screen", "ToggleCursor"),
        new Claim("Hardware", "SetMonitorState"),
        new Claim("Execution", "Scripts"),
        new Claim("Security", "LockWorkStation"),
        new Claim("Security", "LogOffUser"),
        new Claim("Security", "SendCtrlAltDel"),
        new Claim("HostManagement", "Terminate"),
        new Claim("HostManagement", "View"),
        new Claim("HostManagement", "Move"),
        new Claim("HostManagement", "Remove"),
        new Claim("Power", "Reboot"),
        new Claim("Power", "Shutdown"),
        new Claim("Power", "WakeUp"),
        new Claim("Connect", "Control"),
        new Claim("Connect", "View")
    ];

    private static readonly List<Claim> ViewerClaims =
    [
        new Claim("Connect", "View"),
        new Claim("Screen", "ToggleCursor"),
        new Claim("Screen", "SetQuality"),
        new Claim("Screen", "SetFrameRate"),
        new Claim("Screen", "SetCodec"),
        new Claim("Screen", "Change")
    ];

    private static readonly List<Claim> ServiceUserClaims =
    [
        new Claim("CertificateManagement", "Renew")
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
