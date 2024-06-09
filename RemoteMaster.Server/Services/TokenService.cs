// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IdentityModel.Tokens.Jwt;
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
using Serilog;

namespace RemoteMaster.Server.Services;

public class TokenService(IOptions<JwtOptions> options, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : ITokenService
{
    private readonly JwtOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    private static readonly TimeSpan AccessTokenExpiration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan RefreshTokenExpiration = TimeSpan.FromDays(1);

    public async Task<TokenData> GenerateTokensAsync(string userId, string? oldRefreshToken = null)
    {
        var user = await userManager.FindByIdAsync(userId) ?? throw new ArgumentNullException(nameof(userId), "User not found");

        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var claims = await GetClaimsForUser(user);
        var accessToken = await GenerateAccessTokenAsync(claims);
        var refreshToken = oldRefreshToken == null ? await GenerateRefreshTokenAsync(user.Id, ipAddress) : await ReplaceRefreshToken(oldRefreshToken, user.Id, ipAddress);

        return CreateTokenData(accessToken, refreshToken.Token);
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

    private async Task<List<Claim>> GetClaimsForUser(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var userRoles = await userManager.GetRolesAsync(user);

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));

            var roleClaims = await GetClaimsForRole(role);
            claims.AddRange(roleClaims);
        }

        return claims;
    }

    private async Task<string> GenerateAccessTokenAsync(List<Claim> claims)
    {
        if (claims == null || claims.Count == 0)
        {
            throw new ArgumentException("Claims cannot be null or empty", nameof(claims));
        }

#pragma warning disable CA2000
        var rsa = RSA.Create();
#pragma warning restore CA2000

        try
        {
            var passphraseBytes = Encoding.UTF8.GetBytes(_options.KeyPassword);
            var privateKeyPath = Path.Combine(_options.KeysDirectory, "private_key.der");
            var privateKeyBytes = await File.ReadAllBytesAsync(privateKeyPath);

            rsa.ImportEncryptedPkcs8PrivateKey(passphraseBytes, privateKeyBytes, out _);
        }
        catch (CryptographicException ex)
        {
            Log.Error(ex, "Failed to decrypt or import the private key.");
            
            throw;
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

        return tokenHandler.WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.Add(RefreshTokenExpiration),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        return refreshToken;
    }

    private async Task<RefreshToken> ReplaceRefreshToken(string refreshToken, string userId, string ipAddress)
    {
        var refreshTokenEntity = await context.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        if (refreshTokenEntity == null || refreshTokenEntity.Revoked.HasValue || refreshTokenEntity.IsExpired)
        {
            Log.Warning("Refresh token is invalid, revoked, or expired.");
           
            throw new SecurityTokenException("Invalid refresh token.");
        }

        var newRefreshTokenEntity = await GenerateRefreshTokenAsync(userId, ipAddress);

        DetachEntityIfTracked(refreshTokenEntity);

        refreshTokenEntity.Revoked = DateTime.UtcNow;
        refreshTokenEntity.RevokedByIp = ipAddress;
        refreshTokenEntity.RevocationReason = TokenRevocationReason.ReplacedDuringRefresh;
        refreshTokenEntity.ReplacedByToken = newRefreshTokenEntity;

        context.RefreshTokens.Update(refreshTokenEntity);
        await context.SaveChangesAsync();

        return newRefreshTokenEntity;
    }

    private void DetachEntityIfTracked(RefreshToken entity)
    {
        var existingEntity = context.ChangeTracker.Entries<RefreshToken>().FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (existingEntity != null)
        {
            Log.Debug("Detaching already tracked entity with Id: {RefreshTokenId}", entity.Id);
            
            existingEntity.State = EntityState.Detached;
        }
    }

    private async Task<IEnumerable<Claim>> GetClaimsForRole(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        var roleClaims = await roleManager.GetClaimsAsync(role);

        return roleClaims.Where(c => c.Type == "Permission");
    }

    private void RevokeToken(RefreshToken token, TokenRevocationReason reason)
    {
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.RevocationReason = reason;

        context.RefreshTokens.Update(token);
    }

    public async Task RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason)
    {
        var now = DateTime.UtcNow;
        var refreshTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.Revoked == null && rt.Expires > now)
            .ToListAsync();

        if (refreshTokens.Count != 0)
        {
            foreach (var token in refreshTokens)
            {
                RevokeToken(token, revocationReason);
            }

            await context.SaveChangesAsync();

            Log.Information($"All refresh tokens for user {userId} have been revoked. Reason: {revocationReason}");
        }
        else
        {
            Log.Information("No active refresh tokens found for revocation.");
        }
    }

    public async Task CleanUpExpiredRefreshTokens()
    {
        Log.Debug("Starting cleanup of expired and revoked refresh tokens.");

        var expiredTokens = await context.RefreshTokens
            .Where(rt => rt.Expires < DateTime.UtcNow || rt.Revoked.HasValue)
            .ToListAsync();

        if (expiredTokens.Count != 0)
        {
            context.RefreshTokens.RemoveRange(expiredTokens);
            
            await context.SaveChangesAsync();

            foreach (var token in expiredTokens)
            {
                Log.Debug($"Removed token: {token.Token}, User ID: {token.UserId}, Expired at: {token.Expires}, Revoked at: {token.Revoked}");
            }

            Log.Information($"Cleaned up {expiredTokens.Count} expired or revoked refresh tokens.");
        }
        else
        {
            Log.Information("No expired or revoked refresh tokens found to clean up.");
        }
    }

    public bool IsTokenValid(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return false;
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
            
            return validatedToken != null;
        }
        catch
        {
            return false;
        }
    }

    private RSA GetPublicKey()
    {
        var rsa = RSA.Create();

        var publicKeyPath = Path.Combine(_options.KeysDirectory, "public_key.der");
        var publicKeyBytes = File.ReadAllBytes(publicKeyPath);

        rsa.ImportRSAPublicKey(publicKeyBytes, out _);

        return rsa;
    }

    public bool IsRefreshTokenValid(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return false;
        }

        var tokenEntity = context.RefreshTokens
            .AsNoTracking()
            .SingleOrDefault(rt => rt.Token == refreshToken);

        return tokenEntity?.IsActive ?? false;
    }
}
