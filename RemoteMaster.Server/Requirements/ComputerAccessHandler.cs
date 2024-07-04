// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Requirements;

public class ComputerAccessHandler(IServiceScopeFactory scopeFactory) : AuthorizationHandler<ComputerAccessRequirement>
{
    protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, ComputerAccessRequirement requirement)
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

        var computer = await dbContext.Computers
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Name == requirement.Host || c.IpAddress == requirement.Host);

        if (computer == null || computer.ParentId == null)
        {
            context.Fail();

            return;
        }

        var organizationalUnitId = computer.ParentId.Value;

        var hasAccess = await dbContext.OrganizationalUnits
            .Where(ou => ou.NodeId == organizationalUnitId && ou.AccessibleUsers.Any(u => u.Id == userId))
            .AnyAsync();

        if (hasAccess)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
