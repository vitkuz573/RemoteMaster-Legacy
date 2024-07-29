// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RemoteMaster.Host.Core.Requirements;
using RemoteMaster.Shared.Claims;

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
        var isLocal = ipAddress != null && IPAddress.IsLoopback(ipAddress);

        var hasServiceFlagHeader = httpContext.Request.Headers.TryGetValue("X-Service-Flag", out var headerValue) && bool.TryParse(headerValue, out var flagValue) && flagValue;

        if ((!isLocal || !hasServiceFlagHeader) && !(context.User.Identity?.IsAuthenticated ?? false))
        {
            return Task.CompletedTask;
        }

        if (isLocal && !(context.User?.Identity?.IsAuthenticated ?? false))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "RCHost"),
                new(ClaimTypes.Role, "Windows Service"),
                new(CustomClaimTypes.AuthType, "RemoteMaster Security")
            };

            var identity = new ClaimsIdentity(claims, "RemoteMaster Security");

            context.User?.AddIdentity(identity);
        }

        context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
