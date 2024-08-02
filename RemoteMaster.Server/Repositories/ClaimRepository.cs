// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Repositories;

public static class ClaimRepository
{
    public static readonly List<ApplicationClaim> AllClaims =
    [
        new ApplicationClaim { ClaimType = "Input", ClaimValue = "MouseInput", Description = "Allow mouse input control" },
        new ApplicationClaim { ClaimType = "Input", ClaimValue = "KeyboardInput", Description = "Allow keyboard input control" },
        new ApplicationClaim { ClaimType = "Input", ClaimValue = "ToggleInput", Description = "Toggle input control" },
        new ApplicationClaim { ClaimType = "Input", ClaimValue = "BlockUserInput", Description = "Block user input" },
        new ApplicationClaim { ClaimType = "Screen", ClaimValue = "SetFrameRate", Description = "Set screen frame rate" },
        new ApplicationClaim { ClaimType = "Screen", ClaimValue = "SetImageQuality", Description = "Set screen image quality" },
        new ApplicationClaim { ClaimType = "Screen", ClaimValue = "Recording", Description = "Screen recording" },
        new ApplicationClaim { ClaimType = "Screen", ClaimValue = "ToggleDrawCursor", Description = "Toggle drawing cursor" },
        new ApplicationClaim { ClaimType = "Screen", ClaimValue = "ChangeSelectedScreen", Description = "Change selected screen" },
        new ApplicationClaim { ClaimType = "Screen", ClaimValue = "ToggleUseSkia", Description = "Toggle use of Skia" },
        new ApplicationClaim { ClaimType = "Screen", ClaimValue = "SetCodec", Description = "Set screen codec" },
        new ApplicationClaim { ClaimType = "Power", ClaimValue = "RebootComputer", Description = "Reboot the computer" },
        new ApplicationClaim { ClaimType = "Power", ClaimValue = "ShutdownComputer", Description = "Shutdown the computer" },
        new ApplicationClaim { ClaimType = "Power", ClaimValue = "WakeUpComputer", Description = "Wake up the computer" },
        new ApplicationClaim { ClaimType = "Hardware", ClaimValue = "SetMonitorState", Description = "Set monitor state" },
        new ApplicationClaim { ClaimType = "Security", ClaimValue = "LockWorkStation", Description = "Lock the workstation" },
        new ApplicationClaim { ClaimType = "Security", ClaimValue = "LogOffUser", Description = "Log off the user" },
        new ApplicationClaim { ClaimType = "HostManagement", ClaimValue = "TerminateHost", Description = "Terminate the host" },
        new ApplicationClaim { ClaimType = "HostManagement", ClaimValue = "Move", Description = "Move the host" },
        new ApplicationClaim { ClaimType = "HostManagement", ClaimValue = "Remove", Description = "Remove the host" },
        new ApplicationClaim { ClaimType = "HostManagement", ClaimValue = "RenewCertificate", Description = "Renew the certificate" },
        new ApplicationClaim { ClaimType = "Execution", ClaimValue = "Scripts", Description = "Execute scripts" },
        new ApplicationClaim { ClaimType = "Execution", ClaimValue = "ManagePsExecRules", Description = "Manage PsExec rules" },
        new ApplicationClaim { ClaimType = "Execution", ClaimValue = "OpenShell", Description = "Open shell" },
        new ApplicationClaim { ClaimType = "HostManagement", ClaimValue = "Update", Description = "Update the host" },
        new ApplicationClaim { ClaimType = "Domain", ClaimValue = "Membership", Description = "Manage domain membership" },
        new ApplicationClaim { ClaimType = "Communication", ClaimValue = "MessageBox", Description = "Show message box" },
        new ApplicationClaim { ClaimType = "Tasks", ClaimValue = "Manager", Description = "Open task manager" },
        new ApplicationClaim { ClaimType = "Files", ClaimValue = "Manager", Description = "Open file manager" },
        new ApplicationClaim { ClaimType = "Files", ClaimValue = "Upload", Description = "Upload files" },
        new ApplicationClaim { ClaimType = "HostInformation", ClaimValue = "View", Description = "View host information" },
        new ApplicationClaim { ClaimType = "Connect", ClaimValue = "Control", Description = "Control connection" },
        new ApplicationClaim { ClaimType = "Connect", ClaimValue = "View", Description = "View connection" },
        new ApplicationClaim { ClaimType = "Service", ClaimValue = "DisconnectClient", Description = "Disconnect any client" },
        new ApplicationClaim { ClaimType = "Logs", ClaimValue = "Manager", Description = "Open logs manager" }
    ];
}
