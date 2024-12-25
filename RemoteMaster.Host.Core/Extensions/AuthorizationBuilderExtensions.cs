// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using RemoteMaster.Host.Core.Requirements;

namespace RemoteMaster.Host.Core.Extensions;

public static class AuthorizationBuilderExtensions
{
    public static AuthorizationBuilder AddCorePolicies(this AuthorizationBuilder builder)
    {
        builder.AddFileManagerPolicies();
        builder.AddLogHubPolicies();
        builder.AddScreenRecorderPolicies();
        builder.AddTaskManagerPolicies();
        builder.AddUpdaterHubPolicies();
        builder.AddDomainMembershipPolicies();
        builder.AddChatHubPolicies();
        builder.AddCertificateHubPolicies();
        builder.AddControlHubPolicies();

        return builder;
    }

    public static AuthorizationBuilder AddCoreRequirements(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("LocalhostOrAuthenticatedPolicy", policy =>
            policy.Requirements.Add(new LocalhostOrAuthenticatedRequirement()));

        return builder;
    }

    private static AuthorizationBuilder AddFileManagerPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("UploadFilePolicy", policy =>
            policy.RequireClaim("FileManagement", "Upload"));

        builder.AddPolicy("DownloadFilePolicy", policy =>
            policy.RequireClaim("FileManagement", "Download"));

        builder.AddPolicy("ViewFilesPolicy", policy =>
            policy.RequireClaim("FileManagement", "View"));

        builder.AddPolicy("GetDrivesPolicy", policy =>
            policy.RequireClaim("FileManagement", "GetDrives"));

        return builder;
    }

    private static AuthorizationBuilder AddLogHubPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("ViewLogFilesPolicy", policy =>
            policy.RequireClaim("LogManagement", "ViewFiles"));

        builder.AddPolicy("ViewLogContentPolicy", policy =>
            policy.RequireClaim("LogManagement", "ViewContent"));

        builder.AddPolicy("FilterLogPolicy", policy =>
            policy.RequireClaim("LogManagement", "Filter"));

        builder.AddPolicy("DeleteLogsPolicy", policy =>
            policy.RequireClaim("LogManagement", "Delete"));

        return builder;
    }

    private static AuthorizationBuilder AddScreenRecorderPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("StartScreenRecordingPolicy", policy =>
            policy.RequireClaim("ScreenRecording", "Start"));

        builder.AddPolicy("StopScreenRecordingPolicy", policy =>
            policy.RequireClaim("ScreenRecording", "Stop"));

        return builder;
    }

    private static AuthorizationBuilder AddTaskManagerPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("ViewProcessesPolicy", policy =>
            policy.RequireClaim("TaskManagement", "View"));

        builder.AddPolicy("KillProcessPolicy", policy =>
            policy.RequireClaim("TaskManagement", "Kill"));

        builder.AddPolicy("StartProcessPolicy", policy =>
            policy.RequireClaim("TaskManagement", "Start"));

        return builder;
    }

    private static AuthorizationBuilder AddUpdaterHubPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("StartUpdaterPolicy", policy =>
            policy.RequireClaim("UpdaterManagement", "Start"));

        return builder;
    }

    private static AuthorizationBuilder AddDomainMembershipPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("JoinDomainPolicy", policy =>
            policy.RequireClaim("DomainManagement", "Join"));

        builder.AddPolicy("UnjoinDomainPolicy", policy =>
            policy.RequireClaim("DomainManagement", "Unjoin"));

        return builder;
    }

    private static AuthorizationBuilder AddChatHubPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("SendMessagePolicy", policy =>
            policy.RequireClaim("ChatManagement", "Send"));

        builder.AddPolicy("DeleteMessagePolicy", policy =>
            policy.RequireClaim("ChatManagement", "Delete"));

        return builder;
    }

    private static AuthorizationBuilder AddCertificateHubPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("RenewCertificatePolicy", policy =>
            policy.RequireClaim("CertificateManagement", "Renew"));

        builder.AddPolicy("GetCertificateSerialNumberPolicy", policy =>
            policy.RequireClaim("CertificateManagement", "GetSerialNumber"));

        return builder;
    }

    private static AuthorizationBuilder AddControlHubPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("DisconnectClientPolicy", policy =>
            policy.RequireClaim("Service", "DisconnectClient"));

        builder.AddPolicy("HandleInputPolicy", policy =>
            policy.RequireClaim("Input", "Handle"));

        builder.AddPolicy("ChangeScreenPolicy", policy =>
            policy.RequireClaim("Screen", "Change"));

        builder.AddPolicy("SetFrameRatePolicy", policy =>
            policy.RequireClaim("Screen", "SetFrameRate"));

        builder.AddPolicy("SetImageQualityPolicy", policy =>
            policy.RequireClaim("Screen", "SetQuality"));

        builder.AddPolicy("SetCodecPolicy", policy =>
            policy.RequireClaim("Screen", "SetCodec"));

        builder.AddPolicy("ToggleIsCursorVisiblePolicy", policy =>
            policy.RequireClaim("Screen", "ToggleCursor"));

        builder.AddPolicy("ToggleInputPolicy", policy =>
            policy.RequireClaim("Input", "Toggle"));

        builder.AddPolicy("BlockUserInputPolicy", policy =>
            policy.RequireClaim("Input", "Block"));

        builder.AddPolicy("SetMonitorStatePolicy", policy =>
            policy.RequireClaim("Hardware", "SetMonitorState"));

        builder.AddPolicy("ExecuteScriptPolicy", policy =>
            policy.RequireClaim("Execution", "Scripts"));

        builder.AddPolicy("LockWorkStationPolicy", policy =>
            policy.RequireClaim("Security", "LockWorkStation"));

        builder.AddPolicy("LogOffUserPolicy", policy =>
            policy.RequireClaim("Security", "LogOffUser"));

        builder.AddPolicy("TerminateHostPolicy", policy =>
            policy.RequireClaim("HostManagement", "Terminate"));

        builder.AddPolicy("MoveHostPolicy", policy =>
            policy.RequireClaim("HostManagement", "Move"));

        builder.AddPolicy("RebootHostPolicy", policy =>
            policy.RequireClaim("Power", "Reboot"));

        builder.AddPolicy("ShutdownHostPolicy", policy =>
            policy.RequireClaim("Power", "Shutdown"));

        builder.AddPolicy("AudioStreamingPolicy", policy =>
            policy.RequireClaim("Audio", "StartStop"));

        return builder;
    }
}
