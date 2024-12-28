// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Requirements;

namespace RemoteMaster.Server.AuthorizationHandlers;

public class HostAccessHandler(IApplicationUnitOfWork applicationUnitOfWork) : AuthorizationHandler<HostAccessRequirement>
{
    protected async override Task HandleRequirementAsync(AuthorizationHandlerContext context, HostAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        try
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();

                return;
            }

            var user = await applicationUnitOfWork.ApplicationUsers.GetByIdAsync(userId);

            if (user == null)
            {
                context.Fail();

                return;
            }

            if (user.CanAccessUnregisteredHosts)
            {
                var isHostRegistered = await applicationUnitOfWork.Organizations.FindHostsAsync(h => h.Name == requirement.Host || h.IpAddress.Equals(IPAddress.Parse(requirement.Host)));

                if (!isHostRegistered.Any())
                {
                    context.Succeed(requirement);

                    return;
                }
            }

            var hosts = await applicationUnitOfWork.Organizations.FindHostsAsync(h => h.Name == requirement.Host || h.IpAddress.Equals(IPAddress.Parse(requirement.Host)));
            var host = hosts.FirstOrDefault();

            if (host == null)
            {
                context.Fail();

                return;
            }

            var organizationalUnit = await applicationUnitOfWork.Organizations.GetOrganizationalUnitByIdAsync(host.ParentId);

            if (organizationalUnit == null)
            {
                context.Fail();

                return;
            }

            if (organizationalUnit.UserOrganizationalUnits.All(uou => uou.UserId != userId))
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
