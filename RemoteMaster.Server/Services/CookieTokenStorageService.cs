// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class CookieTokenStorageService(IHttpContextAccessor httpContextAccessor) : ITokenStorageService
{
    private const string AccessTokenCookieName = "AccessToken";
    private const string RefreshTokenCookieName = "RefreshToken";

    public Task<Result<string?>> GetAccessTokenAsync(string userId)
    {
        var accessToken = GetCookieValue(AccessTokenCookieName);
            
        return Task.FromResult(Result.Ok(accessToken));
    }

    public Task<Result<string?>> GetRefreshTokenAsync(string userId)
    {
        var refreshToken = GetCookieValue(RefreshTokenCookieName);
            
        return Task.FromResult(Result.Ok(refreshToken));
    }

    public Task<Result> StoreTokensAsync(string userId, TokenData tokenData)
    {
        ArgumentNullException.ThrowIfNull(tokenData);

        SetCookie(AccessTokenCookieName, tokenData.AccessToken, tokenData.AccessTokenExpiresAt);
        SetCookie(RefreshTokenCookieName, tokenData.RefreshToken, tokenData.RefreshTokenExpiresAt);

        return Task.FromResult(Result.Ok());
    }

    public Task<Result> ClearTokensAsync(string userId)
    {
        DeleteCookie(AccessTokenCookieName);
        DeleteCookie(RefreshTokenCookieName);

        return Task.FromResult(Result.Ok());
    }

    private string? GetCookieValue(string cookieName)
    {
        var cookies = httpContextAccessor.HttpContext?.Request.Cookies;
        
        return cookies != null && cookies.TryGetValue(cookieName, out var value) ? value : null;
    }

    private void SetCookie(string cookieName, string? value, DateTime expiresAt)
    {
        if (value == null)
        {
            return;
        }

        var isHttps = httpContextAccessor.HttpContext?.Request.IsHttps ?? false;

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            Expires = expiresAt,
            SameSite = SameSiteMode.Strict
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, value, options);
    }

    private void DeleteCookie(string cookieName)
    {
        var isHttps = httpContextAccessor.HttpContext?.Request.IsHttps ?? false;

        var options = new CookieOptions
        {
            Expires = DateTime.UtcNow.AddDays(-1),
            HttpOnly = true,
            Secure = isHttps,
            SameSite = SameSiteMode.Strict
        };

        httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, "", options);
    }
}
