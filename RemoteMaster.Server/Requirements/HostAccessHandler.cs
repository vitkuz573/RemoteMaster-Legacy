// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Requirements;

public class HostAccessHandler(IServiceScopeFactory scopeFactory) : AuthorizationHandler<HostAccessRequirement>
{
    protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, HostAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            context.Fail();

            return;
        }

        var user = await dbContext.Users.FindAsync(userId);

        if (user == null)
        {
            context.Fail();

            return;
        }

        if (user.CanAccessUnregisteredHosts)
        {
            var isHostRegistered = await dbContext.Computers.AnyAsync(c => c.Name == requirement.Host || c.IpAddress == requirement.Host);

            if (!isHostRegistered)
            {
                context.Succeed(requirement);

                return;
            }
        }

        var computer = await dbContext.Computers
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Name == requirement.Host || c.IpAddress == requirement.Host);

        if (computer?.ParentId == null)
        {
            context.Fail();

            return;
        }

        var organizationalUnit = await dbContext.OrganizationalUnits
            .Include(ou => ou.UserOrganizationalUnits)
            .FirstOrDefaultAsync(ou => ou.Id == computer.ParentId);

        if (organizationalUnit == null || organizationalUnit.UserOrganizationalUnits.All(uou => uou.UserId != userId))
        {
            context.Fail();

            return;
        }

        context.Succeed(requirement);
    }
}
