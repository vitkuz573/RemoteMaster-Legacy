// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Requirements;

namespace RemoteMaster.Server.Services;

public class HostAccessService(IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor, IAccessTokenProvider accessTokenProvider, ILogger<HostAccessService> logger) : IHostAccessService
{
    private async Task<bool> HasAccessAsync(string host)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user == null)
        {
            return false;
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(user, host, new HostAccessRequirement(host));

        return authorizationResult.Succeeded;
    }

    public async Task<AccessInitializationResult> InitializeAccessAsync(string host, ClaimsPrincipal? user)
    {
        var result = new AccessInitializationResult();

        if (user == null || !await HasAccessAsync(host))
        {
            result.IsAccessDenied = true;
            result.ErrorMessage = "Access denied. You do not have permission to access this host.";

            return result;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");
        var tokenResult = await accessTokenProvider.GetAccessTokenAsync(userId);

        if (tokenResult.IsSuccess)
        {
            return result;
        }

        logger.LogWarning("Access token retrieval failed before rendering.");

        result.IsAccessDenied = true;

        result.ErrorMessage = "Authorization failed. Please log in again.";

        return result;
    }
}
