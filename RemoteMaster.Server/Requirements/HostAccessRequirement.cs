// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;

namespace RemoteMaster.Server.Requirements;

public class HostAccessRequirement(string host) : IAuthorizationRequirement
{
    public string Host { get; } = host;
}