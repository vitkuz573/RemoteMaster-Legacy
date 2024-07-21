// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IdentityModel.Tokens.Jwt;
using System.IO.Abstractions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
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
            return Result<TokenData>.Failure("User not found");
        }

        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var claimsResult = await claimsService.GetClaimsForUserAsync(user);
        
        if (!claimsResult.IsSuccess)
        {
            return Result<TokenData>.Failure([.. claimsResult.Errors]);
        }

        var accessTokenResult = await GenerateAccessTokenAsync(claimsResult.Value);
        
        if (!accessTokenResult.IsSuccess)
        {
            return Result<TokenData>.Failure([.. accessTokenResult.Errors]);
        }

        var refreshTokenResult = oldRefreshToken == null
            ? await GenerateRefreshTokenAsync(user, ipAddress)
            : await ReplaceRefreshToken(oldRefreshToken, user, ipAddress);

        if (!refreshTokenResult.IsSuccess)
        {
            return Result<TokenData>.Failure([.. refreshTokenResult.Errors]);
        }

        return Result<TokenData>.Success(CreateTokenData(accessTokenResult.Value, refreshTokenResult.Value.Token));
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
        if (claims == null || claims.Count == 0)
        {
            return Result<string>.Failure("Claims cannot be null or empty");
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
            Log.Error(ex, "Failed to decrypt or import the private key.");
            return Result<string>.Failure("Failed to decrypt or import the private key.", exception: ex);
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

        return Result<string>.Success(tokenHandler.WriteToken(token));
    }

    private async Task<Result<RefreshToken>> GenerateRefreshTokenAsync(ApplicationUser user, string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.Add(RefreshTokenExpiration),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        user.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        return Result<RefreshToken>.Success(refreshToken);
    }

    private async Task<Result<RefreshToken>> ReplaceRefreshToken(string refreshToken, ApplicationUser user, string ipAddress)
    {
        var refreshTokenEntity = user.RefreshTokens.SingleOrDefault(rt => rt.Token == refreshToken);

        if (refreshTokenEntity is not { Revoked: null } || refreshTokenEntity.IsExpired)
        {
            Log.Warning("Refresh token is invalid, revoked, or expired.");
            return Result<RefreshToken>.Failure("Invalid refresh token.");
        }

        var newRefreshTokenResult = await GenerateRefreshTokenAsync(user, ipAddress);

        if (!newRefreshTokenResult.IsSuccess)
        {
            return Result<RefreshToken>.Failure(newRefreshTokenResult.Errors.ToArray());
        }

        refreshTokenEntity.Revoked = DateTime.UtcNow;
        refreshTokenEntity.RevokedByIp = ipAddress;
        refreshTokenEntity.RevocationReason = TokenRevocationReason.Replaced;
        refreshTokenEntity.ReplacedByToken = newRefreshTokenResult.Value;

        await context.SaveChangesAsync();

        return Result<RefreshToken>.Success(newRefreshTokenResult.Value);
    }

    private async Task<Result> RevokeToken(RefreshToken token, TokenRevocationReason reason)
    {
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.RevocationReason = reason;

        var user = context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefault(u => u.RefreshTokens.Any(rt => rt.Token == token.Token));

        if (user != null)
        {
            var refreshTokenEntity = user.RefreshTokens.SingleOrDefault(rt => rt.Token == token.Token);

            if (refreshTokenEntity != null)
            {
                refreshTokenEntity.Revoked = DateTime.UtcNow;
                refreshTokenEntity.RevokedByIp = ipAddress;
                refreshTokenEntity.RevocationReason = reason;
            }
        }

        await context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason)
    {
        var user = await userManager.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result.Failure("User not found");
        }

        var now = DateTime.UtcNow;
        var refreshTokens = user.RefreshTokens
            .Where(rt => rt.Revoked == null && rt.Expires > now)
            .ToList();

        if (refreshTokens.Count == 0)
        {
            Log.Information("No active refresh tokens found for revocation.");
            return Result.Failure("No active refresh tokens found for revocation.");
        }

        foreach (var token in refreshTokens)
        {
            var result = await RevokeToken(token, revocationReason);
            if (!result.IsSuccess)
            {
                return result;
            }
        }

        await context.SaveChangesAsync();
        Log.Information($"All refresh tokens for user {userId} have been revoked. Reason: {revocationReason}");

        return Result.Success();
    }

    public async Task<Result> CleanUpExpiredRefreshTokens()
    {
        Log.Debug("Starting cleanup of expired and revoked refresh tokens.");

        var expiredTokens = await context.Users
            .SelectMany(u => u.RefreshTokens)
            .Where(rt => rt.Expires < DateTime.UtcNow || rt.Revoked.HasValue)
            .ToListAsync();

        if (expiredTokens.Count == 0)
        {
            Log.Information("No expired or revoked tokens found for cleanup.");
            return Result.Failure("No expired or revoked tokens found for cleanup.");
        }

        context.RemoveRange(expiredTokens);

        foreach (var token in expiredTokens)
        {
            Log.Debug($"Removed token: {token.Token}, User ID: {token.UserId}, Expired at: {token.Expires}, Revoked at: {token.Revoked}");
        }

        await context.SaveChangesAsync();

        Log.Information("Cleaned up expired or revoked refresh tokens.");
        
        return Result.Success();
    }

    public Result<bool> IsTokenValid(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return Result<bool>.Failure("Access token cannot be null or empty");
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

            return Result<bool>.Success(validatedToken != null);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Token validation failed");
            
            return Result<bool>.Failure("Token validation failed", exception: ex);
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

    public Result<bool> IsRefreshTokenValid(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result<bool>.Failure("Refresh token cannot be null or empty");
        }

        var user = context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefault(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken));

        var tokenEntity = user?.RefreshTokens.SingleOrDefault(rt => rt.Token == refreshToken);

        return Result<bool>.Success(tokenEntity?.IsActive ?? false);
    }
}
