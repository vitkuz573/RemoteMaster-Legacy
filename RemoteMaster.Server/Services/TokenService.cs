// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IdentityModel.Tokens.Jwt;
using System.IO.Abstractions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Options;
using RemoteMaster.Server.ValueObjects;
using Serilog;

namespace RemoteMaster.Server.Services;

public class TokenService(IOptions<JwtOptions> options, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, IClaimsService claimsService, IFileSystem fileSystem) : ITokenService
{
    private readonly JwtOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

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

        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var claimsResult = await claimsService.GetClaimsForUserAsync(user);

        if (claimsResult.IsFailed)
        {
            return Result.Fail<TokenData>(claimsResult.Errors.Select(e => e.Message).ToArray());
        }

        var accessTokenResult = await GenerateAccessTokenAsync(claimsResult.Value);

        if (accessTokenResult.IsFailed)
        {
            return Result.Fail<TokenData>(accessTokenResult.Errors.Select(e => e.Message).ToArray());
        }

        var refreshTokenResult = oldRefreshToken == null
            ? await GenerateRefreshTokenAsync(user, ipAddress)
            : await ReplaceRefreshToken(oldRefreshToken, user, ipAddress);

        return refreshTokenResult.IsFailed
            ? Result.Fail<TokenData>(refreshTokenResult.Errors.Select(e => e.Message).ToArray())
            : Result.Ok(CreateTokenData(accessTokenResult.Value, refreshTokenResult.Value.TokenValue.Token));
    }

    private static TokenData CreateTokenData(string accessToken, string refreshToken)
    {
        return new TokenData
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.Add(AccessTokenExpiration),
            RefreshTokenExpiresAt = DateTime.UtcNow.Add(RefreshTokenExpiration)
        };
    }

    private async Task<Result<string>> GenerateAccessTokenAsync(List<Claim> claims)
    {
        if (claims.IsNullOrEmpty())
        {
            return Result.Fail<string>("Claims cannot be null or empty");
        }

#pragma warning disable CA2000
        var rsa = RSA.Create();
#pragma warning restore CA2000

        try
        {
            var passphraseBytes = Encoding.UTF8.GetBytes(_options.KeyPassword);
            var privateKeyPath = fileSystem.Path.Combine(_options.KeysDirectory, "private_key.der");
            var privateKeyBytes = await fileSystem.File.ReadAllBytesAsync(privateKeyPath);

            rsa.ImportEncryptedPkcs8PrivateKey(passphraseBytes, privateKeyBytes, out _);
        }
        catch (CryptographicException ex)
        {
            Log.Error("Failed to decrypt or import the private key: {Message}.", ex.Message);

            return Result.Fail<string>("Failed to decrypt or import the private key.").WithError(ex.Message);
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = "RemoteMaster Server",
            Audience = "RMServiceAPI",
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(AccessTokenExpiration),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Result.Ok(tokenHandler.WriteToken(token));
    }

    private async Task<Result<RefreshToken>> GenerateRefreshTokenAsync(ApplicationUser user, string ipAddress)
    {
        var tokenValue = new TokenValue(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), DateTime.UtcNow.Add(RefreshTokenExpiration), DateTime.UtcNow, ipAddress);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenValue = tokenValue
        };

        user.RefreshTokens.Add(refreshToken);

        await context.SaveChangesAsync();

        return Result.Ok(refreshToken);
    }

    private async Task<Result<RefreshToken>> ReplaceRefreshToken(string refreshToken, ApplicationUser user, string ipAddress)
    {
        var refreshTokenEntity = user.RefreshTokens.SingleOrDefault(rt => rt.TokenValue.Token == refreshToken);

        if (refreshTokenEntity is not { RevocationInfo.Revoked: null } || refreshTokenEntity.TokenValue.IsExpired)
        {
            Log.Warning("Refresh token is invalid, revoked, or expired.");

            return Result.Fail<RefreshToken>("Invalid refresh token.");
        }

        var newRefreshTokenResult = await GenerateRefreshTokenAsync(user, ipAddress);

        if (newRefreshTokenResult.IsFailed)
        {
            return Result.Fail<RefreshToken>(newRefreshTokenResult.Errors.Select(e => e.Message).ToArray());
        }

        var revocationInfo = new TokenRevocationInfo(DateTime.UtcNow, ipAddress, TokenRevocationReason.Replaced);

        refreshTokenEntity.RevocationInfo = revocationInfo;
        refreshTokenEntity.ReplacedByToken = newRefreshTokenResult.Value;

        await context.SaveChangesAsync();

        return Result.Ok(newRefreshTokenResult.Value);
    }

    private async Task<Result> RevokeToken(RefreshToken token, TokenRevocationReason reason)
    {
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var revocationInfo = new TokenRevocationInfo(DateTime.UtcNow, ipAddress, reason);

        token.RevocationInfo = revocationInfo;

        var user = context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefault(u => u.RefreshTokens.Any(rt => rt.TokenValue.Token == token.TokenValue.Token));

        var refreshTokenEntity = user?.RefreshTokens.SingleOrDefault(rt => rt.TokenValue.Token == token.TokenValue.Token);

        if (refreshTokenEntity != null)
        {
            refreshTokenEntity.RevocationInfo = revocationInfo;
        }

        await context.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason)
    {
        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result.Fail("User not found");
        }

        var now = DateTime.UtcNow;
        var refreshTokens = user.RefreshTokens
            .Where(rt => rt.RevocationInfo.Revoked == null && rt.TokenValue.Expires > now)
            .ToList();

        if (refreshTokens.Count == 0)
        {
            Log.Information("No active refresh tokens found for revocation.");

            return Result.Fail("No active refresh tokens found for revocation.");
        }

        foreach (var token in refreshTokens)
        {
            var result = await RevokeToken(token, revocationReason);

            if (result.IsFailed)
            {
                return result;
            }
        }

        await context.SaveChangesAsync();

        Log.Information($"All refresh tokens for user {userId} have been revoked. Reason: {revocationReason}");

        return Result.Ok();
    }

    public async Task<Result> CleanUpExpiredRefreshTokens()
    {
        Log.Debug("Starting cleanup of expired and revoked refresh tokens.");

        var expiredTokens = await context.Users
            .SelectMany(u => u.RefreshTokens)
            .Where(rt => rt.TokenValue.Expires < DateTime.UtcNow || rt.RevocationInfo.Revoked.HasValue)
            .ToListAsync();

        if (expiredTokens.Count == 0)
        {
            Log.Information("No expired or revoked tokens found for cleanup.");

            return Result.Fail("No expired or revoked tokens found for cleanup.");
        }

        context.RemoveRange(expiredTokens);

        foreach (var token in expiredTokens)
        {
            Log.Debug($"Removed token: {token.TokenValue.Token}, User ID: {token.UserId}, Expired at: {token.TokenValue.Expires}, Revoked at: {token.RevocationInfo.Revoked}");
        }

        await context.SaveChangesAsync();

        Log.Information("Cleaned up expired or revoked refresh tokens.");

        return Result.Ok();
    }

    public Result IsTokenValid(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return Result.Fail("Access token cannot be null or empty");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
#pragma warning disable CA2000
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(GetPublicKey()),
            ValidateIssuer = true,
            ValidIssuer = "RemoteMaster Server",
            ValidateAudience = true,
            ValidAudience = "RMServiceAPI",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
#pragma warning restore CA2000

        try
        {
            tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            return validatedToken == null ? Result.Fail("Invalid token") : Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error("Token validation failed: {Message}.", ex.Message);

            return Result.Fail("Token validation failed").WithError(ex.Message);
        }
    }

    private RSA GetPublicKey()
    {
        var rsa = RSA.Create();

        var publicKeyPath = fileSystem.Path.Combine(_options.KeysDirectory, "public_key.der");
        var publicKeyBytes = fileSystem.File.ReadAllBytes(publicKeyPath);

        rsa.ImportRSAPublicKey(publicKeyBytes, out _);

        return rsa;
    }

    public Result IsRefreshTokenValid(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result.Fail("Refresh token cannot be null or empty");
        }

        var user = context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefault(u => u.RefreshTokens.Any(rt => rt.TokenValue.Token == refreshToken));

        var tokenEntity = user?.RefreshTokens.SingleOrDefault(rt => rt.TokenValue.Token == refreshToken);

        return tokenEntity is not { IsActive: true } ? Result.Fail("Invalid or inactive refresh token") : Result.Ok();
    }
}
