// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Requirements;

public class HostAccessHandler(IApplicationUserRepository userRepository, IOrganizationalUnitRepository organizationalUnitRepository) : AuthorizationHandler<HostAccessRequirement>
{
    protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, HostAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();

                return;
            }

            var user = await userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                context.Fail();

                return;
            }

            if (user.CanAccessUnregisteredHosts)
            {
                var isHostRegistered = await organizationalUnitRepository
                    .FindComputersAsync(c => c.Name == requirement.Host || c.IpAddress == requirement.Host);

                if (!isHostRegistered.Any())
                {
                    context.Succeed(requirement);

                    return;
                }
            }

            var computers = await organizationalUnitRepository
                .FindComputersAsync(c => c.Name == requirement.Host || c.IpAddress == requirement.Host);
            var computer = computers.FirstOrDefault();

            if (computer?.ParentId == null)
            {
                context.Fail();

                return;
            }

            var organizationalUnit = await organizationalUnitRepository.GetByIdAsync(computer.ParentId);

            if (organizationalUnit == null || organizationalUnit.UserOrganizationalUnits.All(uou => uou.UserId != userId))
            {
                context.Fail();

                return;
            }

            context.Succeed(requirement);
        }
        catch (Exception)
        {
            context.Fail();
        }
    }
}