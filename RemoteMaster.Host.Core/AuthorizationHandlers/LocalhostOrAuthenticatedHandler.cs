// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RemoteMaster.Host.Core.Requirements;

namespace RemoteMaster.Host.Core.AuthorizationHandlers;

public class LocalhostOrAuthenticatedHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<LocalhostOrAuthenticatedRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalhostOrAuthenticatedRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress;
        var isLocal = ipAddress != null && (ipAddress.Equals(IPAddress.Loopback) || ipAddress.Equals(IPAddress.IPv6Loopback) || ipAddress.ToString().StartsWith("::ffff:127.0.0.1"));

        if (isLocal || (context.User?.Identity?.IsAuthenticated ?? false))
        {
            if (isLocal && !(context.User?.Identity?.IsAuthenticated ?? false))
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, "RCHost"),
                    new(ClaimTypes.Role, "Windows Service")
                };

                var identity = new ClaimsIdentity(claims, "RemoteMaster Security");

                context.User.AddIdentity(identity);
            }

            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
