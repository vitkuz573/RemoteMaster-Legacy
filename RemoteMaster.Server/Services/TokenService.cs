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
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Options;
using Serilog;

namespace RemoteMaster.Server.Services;

public class TokenService(IOptions<JwtOptions> options, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, IClaimsService claimsService, IFileSystem fileSystem, IApplicationUserRepository applicationUserRepository) : ITokenService
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

        var refreshTokenResult = await CreateOrReplaceRefreshTokenAsync(user, ipAddress, oldRefreshToken);

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

    private async Task<Result<RefreshToken>> CreateOrReplaceRefreshTokenAsync(ApplicationUser user, string ipAddress, string? existingToken = null)
    {
        var refreshToken = existingToken != null
            ? user.ReplaceRefreshToken(existingToken, ipAddress)
            : user.AddRefreshToken(DateTime.UtcNow.Add(RefreshTokenExpiration), ipAddress);

        await applicationUserRepository.UpdateAsync(user);
        await applicationUserRepository.SaveChangesAsync();

        return Result.Ok(refreshToken);
    }

    public async Task<Result> RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason)
    {
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var user = await applicationUserRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return Result.Fail("User not found");
        }

        var refreshTokens = user.RefreshTokens
            .Where(rt => rt.RevocationInfo == null && rt.TokenValue.Expires > DateTime.UtcNow)
            .ToList();

        if (refreshTokens.Count == 0)
        {
            Log.Information($"No active refresh tokens found for revocation for user {userId}.");

            return Result.Fail("No active refresh tokens found for revocation.");
        }

        foreach (var token in refreshTokens)
        {
            user.RevokeRefreshToken(token.TokenValue.Token, revocationReason, ipAddress);
        }

        await applicationUserRepository.UpdateAsync(user);
        await applicationUserRepository.SaveChangesAsync();

        Log.Information($"All refresh tokens for user {userId} have been revoked. Reason: {revocationReason}");

        return Result.Ok();
    }

    public async Task<Result> CleanUpExpiredRefreshTokens()
    {
        Log.Debug("Starting cleanup of expired and revoked refresh tokens.");

        var usersWithExpiredTokens = (await applicationUserRepository.FindAsync(u =>
            u.RefreshTokens.Any(rt => rt.TokenValue.Expires < DateTime.UtcNow || rt.RevocationInfo != null))).ToList();

        if (usersWithExpiredTokens.Count == 0)
        {
            Log.Information("No expired or revoked tokens found for cleanup.");

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

                Log.Debug($"Removed token: {token.TokenValue.Token}, User ID: {user.Id}, Expired at: {token.TokenValue.Expires}, Revoked at: {token.RevocationInfo?.Revoked}");
            }

            await applicationUserRepository.UpdateAsync(user);
        }

        await applicationUserRepository.SaveChangesAsync();

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
