// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class AccessTokenProvider(ITokenService tokenService, IHttpContextAccessor httpContextAccessor) : IAccessTokenProvider
{
    public async Task<string> GetAccessTokenAsync()
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
                SetCookie(CookieNames.AccessToken, newTokens.AccessToken, 2);
                SetCookie(CookieNames.RefreshToken, newTokens.RefreshToken, 7 * 24);
               
                return newTokens.AccessToken;
            }
        }

        throw new InvalidOperationException("No valid access token available.");
    }

    private void SetCookie(string key, string value, int expireHours)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddHours(expireHours)
        };

        httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, options);
    }
}

