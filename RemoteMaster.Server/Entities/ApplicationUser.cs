// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Entities;

public class ApplicationUser : IdentityUser, IAggregateRoot
{
    public ICollection<UserOrganization> UserOrganizations { get; } = [];

    public ICollection<UserOrganizationalUnit> UserOrganizationalUnits { get; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; } = [];

    public bool CanAccessUnregisteredHosts { get; set; }

    public SignInEntry AddSignInEntry(bool isSuccessful, string ipAddress)
    {
        return new SignInEntry(Id, DateTime.UtcNow, isSuccessful, ipAddress);
    }
}
