// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using RemoteMaster.Host.Core.Requirements;

namespace RemoteMaster.Host.Core.Extensions;

public static class AuthorizationBuilderExtensions
{
    public static AuthorizationBuilder AddCustomPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("LocalhostOrAuthenticatedPolicy", policy => policy.Requirements.Add(new LocalhostOrAuthenticatedRequirement()));

        builder.AddPolicy("ChangeSelectedScreenPolicy", policy => policy.RequireClaim("Screen", "ChangeSelectedScreen"));
        builder.AddPolicy("SetFrameRatePolicy", policy => policy.RequireClaim("Screen", "SetFrameRate"));
        builder.AddPolicy("SetImageQualityPolicy", policy => policy.RequireClaim("Screen", "SetImageQuality"));
        builder.AddPolicy("ToggleDrawCursorPolicy", policy => policy.RequireClaim("Screen", "ToggleDrawCursor"));
        builder.AddPolicy("SetCodecPolicy", policy => policy.RequireClaim("Screen", "SetCodec"));
        builder.AddPolicy("MouseInputPolicy", policy => policy.RequireClaim("Input", "MouseInput"));
        builder.AddPolicy("KeyboardInputPolicy", policy => policy.RequireClaim("Input", "KeyboardInput"));
        builder.AddPolicy("ToggleInputPolicy", policy => policy.RequireClaim("Input", "ToggleInput"));
        builder.AddPolicy("ToggleClickIndicator", policy => policy.RequireClaim("Input", "ToggleClickIndicator"));
        builder.AddPolicy("BlockUserInputPolicy", policy => policy.RequireClaim("Input", "BlockUserInput"));
        builder.AddPolicy("RebootHostPolicy", policy => policy.RequireClaim("Power", "RebootHost"));
        builder.AddPolicy("ShutdownHostPolicy", policy => policy.RequireClaim("Power", "ShutdownHost"));
        builder.AddPolicy("SetMonitorStatePolicy", policy => policy.RequireClaim("Hardware", "SetMonitorState"));
        builder.AddPolicy("ExecuteScriptPolicy", policy => policy.RequireClaim("Execution", "Scripts"));
        builder.AddPolicy("LockWorkStationPolicy", policy => policy.RequireClaim("Security", "LockWorkStation"));
        builder.AddPolicy("LogOffUserPolicy", policy => policy.RequireClaim("Security", "LogOffUser"));
        builder.AddPolicy("TerminateHostPolicy", policy => policy.RequireClaim("HostManagement", "TerminateHost"));
        builder.AddPolicy("MoveHostPolicy", policy => policy.RequireClaim("HostManagement", "Move"));
        builder.AddPolicy("RenewCertificatePolicy", policy => policy.RequireClaim("HostManagement", "RenewCertificate"));
        builder.AddPolicy("DisconnectClientPolicy", policy => policy.RequireClaim("Service", "DisconnectClient"));

        return builder;
    }
}
