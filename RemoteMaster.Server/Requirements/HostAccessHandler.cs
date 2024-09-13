// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Requirements;

public class HostAccessHandler(IApplicationUserRepository userRepository, IOrganizationRepository organizationRepository) : AuthorizationHandler<HostAccessRequirement>
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
                Log.Information("Access denied: User ID not found in the context.");
                context.Fail();
                return;
            }

            Log.Information("Processing access for user {UserId} to host {Host}", userId, requirement.Host);

            var user = await userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                Log.Information("Access denied: User {UserId} not found in the database.", userId);
                context.Fail();
                return;
            }

            if (user.CanAccessUnregisteredHosts)
            {
                Log.Information("User {UserId} has permission to access unregistered hosts.", userId);

                var isHostRegistered = await organizationRepository.FindComputersAsync(c => c.Name == requirement.Host || c.IpAddress == requirement.Host);

                if (!isHostRegistered.Any())
                {
                    Log.Information("Access granted: Host {Host} is unregistered, user {UserId} is allowed access.", requirement.Host, userId);
                    context.Succeed(requirement);
                    return;
                }
            }

            var computers = await organizationRepository.FindComputersAsync(c => c.Name == requirement.Host || c.IpAddress == requirement.Host);
            var computer = computers.FirstOrDefault();

            if (computer == null)
            {
                Log.Information("Access denied: Host {Host} not found in the system.", requirement.Host);
                context.Fail();
                return;
            }

            var organizationalUnit = await organizationRepository.GetOrganizationalUnitByIdAsync(computer.ParentId);

            if (organizationalUnit == null)
            {
                Log.Information("Access denied: Organizational unit for host {Host} (ParentId: {ParentId}) not found.", requirement.Host, computer.ParentId);
                context.Fail();
                return;
            }

            if (organizationalUnit.UserOrganizationalUnits.All(uou => uou.UserId != userId))
            {
                Log.Information("Access denied: User {UserId} does not have permission to access organizational unit {OrganizationalUnitId}.", userId, organizationalUnit.Id);
                context.Fail();
                return;
            }

            Log.Information("Access granted: User {UserId} is allowed to access host {Host}.", userId, requirement.Host);
            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Access denied due to an error while processing access for user {UserId} to host {Host}.", context.User?.FindFirstValue(ClaimTypes.NameIdentifier), requirement.Host);
            context.Fail();
        }
    }
}
