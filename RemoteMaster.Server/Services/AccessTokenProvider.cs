// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Extensions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class AccessTokenProvider(ITokenService tokenService, IHttpContextAccessor httpContextAccessor) : IAccessTokenProvider
{
    public async Task<string?> GetAccessTokenAsync()
    {
        var context = httpContextAccessor.HttpContext;
        var accessToken = context.Request.Cookies[CookieNames.AccessToken];

        if (!string.IsNullOrEmpty(accessToken) && tokenService.IsTokenValid(accessToken))
        {
            return accessToken;
        }

        var refreshToken = context.Request.Cookies[CookieNames.RefreshToken];
        
        if (!string.IsNullOrEmpty(refreshToken) && tokenService.IsRefreshTokenValid(refreshToken))
        {
            var newTokens = await tokenService.RefreshAccessToken(refreshToken);
            
            if (newTokens != null && !string.IsNullOrEmpty(newTokens.AccessToken))
            {
                context.SetCookie(CookieNames.AccessToken, newTokens.AccessToken, TimeSpan.FromMinutes(20));
                context.SetCookie(CookieNames.RefreshToken, newTokens.RefreshToken, TimeSpan.FromHours(25));
               
                return newTokens.AccessToken;
            }
        }

        context.Response.Redirect("/Account/Logout");

        return null;
    }
}

