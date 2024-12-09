// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;

namespace RemoteMaster.Host.Windows.Extensions;

public static class AuthorizationBuilderExtensions
{
    public static void AddWindowsPolicies(this AuthorizationBuilder builder)
    {
        builder.AddDeviceManagerPolicies();
        builder.AddRegistryHubPolicies();
    }

    private static AuthorizationBuilder AddDeviceManagerPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("ViewDevicesPolicy", policy =>
            policy.RequireClaim("DeviceManagement", "View"));

        builder.AddPolicy("DisableDevicePolicy", policy =>
            policy.RequireClaim("DeviceManagement", "Disable"));

        builder.AddPolicy("EnableDevicePolicy", policy =>
            policy.RequireClaim("DeviceManagement", "Enable"));

        builder.AddPolicy("UpdateDeviceDriverPolicy", policy =>
            policy.RequireClaim("DeviceManagement", "UpdateDriver"));

        return builder;
    }

    private static AuthorizationBuilder AddRegistryHubPolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicy("GetRootKeysPolicy", policy =>
            policy.RequireClaim("RegistryManagement", "GetRootKeys"));

        builder.AddPolicy("GetRegistryValuePolicy", policy =>
            policy.RequireClaim("RegistryManagement", "GetValue"));

        builder.AddPolicy("SetRegistryValuePolicy", policy =>
            policy.RequireClaim("RegistryManagement", "SetValue"));

        builder.AddPolicy("GetSubKeyNamesPolicy", policy =>
            policy.RequireClaim("RegistryManagement", "GetSubKeys"));

        builder.AddPolicy("GetAllRegistryValuesPolicy", policy =>
            policy.RequireClaim("RegistryManagement", "GetAllValues"));

        builder.AddPolicy("ExportRegistryBranchPolicy", policy =>
            policy.RequireClaim("RegistryManagement", "ExportBranch"));

        return builder;
    }
}
