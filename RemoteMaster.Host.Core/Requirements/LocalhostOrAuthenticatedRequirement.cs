// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;

namespace RemoteMaster.Host.Core.Requirements;

public class LocalhostOrAuthenticatedRequirement : IAuthorizationRequirement
{
}
