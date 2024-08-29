// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Repositories;

public static class ClaimRepository
{
    public static readonly List<ApplicationClaim> AllClaims =
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
        new ApplicationClaim("Screen", "ToggleUseSkia", "Toggle use of Skia"),
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
}
