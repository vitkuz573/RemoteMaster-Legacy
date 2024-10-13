// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class TokenService(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, IClaimsService claimsService, ITokenSigningService tokenSigningService, IApplicationUnitOfWork applicationUnitOfWork, ILogger<TokenService> logger) : ITokenService
{
    private static readonly TimeSpan AccessTokenExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenExpiration = TimeSpan.FromDays(1);

    public async Task<Result<TokenData>> GenerateTokensAsync(string userId, string? oldRefreshToken = null)
    {
        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result.Fail<TokenData>("User not found");
        }

        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress ?? IPAddress.None;

        var claimsResult = await claimsService.GetClaimsForUserAsync(user);

        if (claimsResult.IsFailed)
        {
            return Result.Fail<TokenData>(claimsResult.Errors.Select(e => e.Message).ToArray());
        }

        var accessTokenResult = tokenSigningService.GenerateAccessToken(claimsResult.Value);

        if (accessTokenResult.IsFailed)
        {
            return Result.Fail<TokenData>(accessTokenResult.Errors.Select(e => e.Message).ToArray());
        }

        var refreshTokenResult = oldRefreshToken != null
            ? Result.Ok(user.ReplaceRefreshToken(oldRefreshToken, ipAddress))
            : Result.Ok(user.AddRefreshToken(DateTime.UtcNow.Add(RefreshTokenExpiration), ipAddress));

        if (refreshTokenResult.IsFailed)
        {
            return Result.Fail<TokenData>(refreshTokenResult.Errors.Select(e => e.Message).ToArray());
        }

        applicationUnitOfWork.ApplicationUsers.Update(user);
        await applicationUnitOfWork.CommitAsync();

        return Result.Ok(CreateTokenData(accessTokenResult.Value, refreshTokenResult.Value.TokenValue.Value));
    }

    private static TokenData CreateTokenData(string accessToken, string refreshToken)
    {
        return new TokenData(accessToken, refreshToken, DateTime.UtcNow.Add(AccessTokenExpiration), DateTime.UtcNow.Add(RefreshTokenExpiration));
    }

    public async Task<Result> RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason)
    {
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress ?? IPAddress.None;

        var user = await applicationUnitOfWork.ApplicationUsers.GetByIdAsync(userId);

        if (user == null)
        {
            return Result.Fail("User not found");
        }

        var refreshTokens = user.RefreshTokens
            .Where(rt => rt.RevocationInfo == null && rt.TokenValue.Expires > DateTime.UtcNow)
            .ToList();

        if (refreshTokens.Count == 0)
        {
            logger.LogInformation("No active refresh tokens found for revocation for user {UserId}.", user);

            return Result.Fail("No active refresh tokens found for revocation.");
        }

        foreach (var token in refreshTokens)
        {
            user.RevokeRefreshToken(token.TokenValue.Value, revocationReason, ipAddress);
        }

        applicationUnitOfWork.ApplicationUsers.Update(user);
        await applicationUnitOfWork.CommitAsync();

        logger.LogInformation("All refresh tokens for user {UserId} have been revoked. Reason: {RevocationReason}", userId, revocationReason);

        return Result.Ok();
    }

    public async Task<Result> CleanUpExpiredRefreshTokens()
    {
        logger.LogDebug("Starting cleanup of expired and revoked refresh tokens.");

        var usersWithExpiredTokens = (await applicationUnitOfWork.ApplicationUsers.FindAsync(u =>
            u.RefreshTokens.Any(rt => rt.TokenValue.Expires < DateTime.UtcNow || rt.RevocationInfo != null))).ToList();

        if (usersWithExpiredTokens.Count == 0)
        {
            logger.LogInformation("No expired or revoked tokens found for cleanup.");

            return Result.Fail("No expired or revoked tokens found for cleanup.");
        }

        foreach (var user in usersWithExpiredTokens)
        {
            var expiredTokens = user.RefreshTokens
                .Where(rt => rt.TokenValue.Expires < DateTime.UtcNow || rt.RevocationInfo != null)
                .ToList();

            foreach (var token in expiredTokens)
            {
                user.RemoveRefreshToken(token);

                logger.LogDebug("Removed token: {TokenValue}, User ID: {UserId}, Expired at: {TokenExpires}, Revoked at: {TokenRevoked}", token.TokenValue.Value, user.Id, token.TokenValue.Expires, token.RevocationInfo?.Revoked);
            }

            applicationUnitOfWork.ApplicationUsers.Update(user);
        }

        await applicationUnitOfWork.CommitAsync();

        logger.LogInformation("Cleaned up expired or revoked refresh tokens.");

        return Result.Ok();
    }

    public async Task<Result> IsRefreshTokenValid(string userId, string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result.Fail("Refresh token cannot be null or empty");
        }

        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Id == userId);

        return user != null && user.IsRefreshTokenValid(refreshToken) ? Result.Ok() : Result.Fail("Invalid or inactive refresh token");
    }
}
