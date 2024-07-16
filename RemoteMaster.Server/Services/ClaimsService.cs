// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Services;

public class ClaimsService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : IClaimsService
{
    public async Task<List<Claim>> GetClaimsForUserAsync(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id)
        };

        var userRoles = await userManager.GetRolesAsync(user);

        foreach (var role in userRoles)
        {
            var identityRole = await roleManager.FindByNameAsync(role);

            if (identityRole == null)
            {
                continue;
            }

            claims.Add(new Claim(ClaimTypes.Role, role));

            var roleClaims = await roleManager.GetClaimsAsync(identityRole);
            claims.AddRange(roleClaims);
        }

        return claims;
    }
}
