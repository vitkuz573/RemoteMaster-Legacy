﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Requirements;
using RemoteMaster.Shared.Claims;

namespace RemoteMaster.Host.Core.AuthorizationHandlers;

public class LocalhostOrAuthenticatedHandler : AuthorizationHandler<LocalhostOrAuthenticatedRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPAddress _hostIpAddress;

    public LocalhostOrAuthenticatedHandler(IHttpContextAccessor httpContextAccessor, IHostConfigurationService hostConfigurationService)
    {
        ArgumentNullException.ThrowIfNull(hostConfigurationService);

        _httpContextAccessor = httpContextAccessor;

        var hostConfiguration = hostConfigurationService.LoadAsync().GetAwaiter().GetResult();
        _hostIpAddress = hostConfiguration.Host.IpAddress;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalhostOrAuthenticatedRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress;
        var isLocal = ipAddress != null && IsLocalIpAddress(ipAddress);
        var hasServiceFlagHeader = httpContext.Request.Headers.TryGetValue("Service-Flag", out var headerValue) && bool.TryParse(headerValue, out var flagValue) && flagValue;
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

        if (!isAuthenticated)
        {
            if (!isLocal || !hasServiceFlagHeader)
            {
                return Task.CompletedTask;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "RCHost"),
                new(ClaimTypes.Role, "System Service"),
                new(CustomClaimTypes.AuthType, "RemoteMaster Security")
            };

            var identity = new ClaimsIdentity(claims, "RemoteMaster Security");

            context.User.AddIdentity(identity);
        }

        context.Succeed(requirement);

        return Task.CompletedTask;
    }

    private bool IsLocalIpAddress(IPAddress remoteIpAddress)
    {
        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            remoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        return remoteIpAddress.Equals(_hostIpAddress);
    }
}
