// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;

namespace RemoteMaster.Server.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<UserOrganization> UserOrganizations { get; } = [];

    public ICollection<UserOrganizationalUnit> UserOrganizationalUnits { get; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; } = [];

    public bool CanAccessUnregisteredHosts { get; set; }
}
