// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Data;

public class ApplicationUser : IdentityUser
{
    public ICollection<Organization> AccessibleOrganizations { get; } = [];

    public ICollection<OrganizationalUnit> AccessibleOrganizationalUnits { get; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; } = [];
}
