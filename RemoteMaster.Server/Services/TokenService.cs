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

public class TokenService(IOptions<JwtOptions> options, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager) : ITokenService
{
    private readonly JwtOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public async Task<string> GenerateAccessTokenAsync(List<Claim> claims)
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
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken(string userId, string ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(1),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        context.RefreshTokens.Add(refreshToken);
        context.SaveChanges();

        return refreshToken.Token;
    }

    public bool IsTokenValid(string accessToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        if (string.IsNullOrEmpty(accessToken))
        {
            return false;
        }

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
        catch (SecurityTokenExpiredException)
        {
            return false;
        }
        catch (SecurityTokenValidationException)
        {
            return false;
        }
        catch (Exception)
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

        if (tokenEntity == null)
        {
            return false;
        }

        return tokenEntity.IsActive;
    }

    public async Task<TokenData?> RefreshAccessToken(string refreshToken)
    {
        var refreshTokenEntity = await context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.Revoked.HasValue);

        if (refreshTokenEntity == null || refreshTokenEntity.IsExpired)
        {
            Log.Warning("Refresh token is null or expired for token: {RefreshToken}", refreshToken);
            return null;
        }

        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = refreshTokenEntity.UserId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(1),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        try
        {
            await context.RefreshTokens.AddAsync(newRefreshTokenEntity);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving new refresh token for user: {UserId}", refreshTokenEntity.UserId);
            throw;
        }

        var existingEntity = context.ChangeTracker.Entries<RefreshToken>().FirstOrDefault(e => e.Entity.Id == refreshTokenEntity.Id);
        
        if (existingEntity != null)
        {
            Log.Warning("Detaching already tracked entity with Id: {RefreshTokenId}", refreshTokenEntity.Id);
            existingEntity.State = EntityState.Detached;
        }

        refreshTokenEntity.Revoked = DateTime.UtcNow;
        refreshTokenEntity.RevokedByIp = ipAddress;
        refreshTokenEntity.RevocationReason = TokenRevocationReason.ReplacedDuringRefresh;
        refreshTokenEntity.ReplacedByToken = newRefreshTokenEntity;

        try
        {
            context.RefreshTokens.Attach(refreshTokenEntity);
            context.Entry(refreshTokenEntity).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating refresh token entity with Id: {RefreshTokenId}", refreshTokenEntity.Id);
            throw;
        }

        var user = await context.Users.FindAsync(refreshTokenEntity.UserId);

        if (user == null)
        {
            Log.Warning("User not found for userId: {UserId}", refreshTokenEntity.UserId);
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        var userRoles = await userManager.GetRolesAsync(user);

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var newAccessToken = await GenerateAccessTokenAsync(claims);

        return new TokenData
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenEntity.Token,
        };
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, TokenRevocationReason revocationReason)
    {
        var refreshTokenEntity = await context.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        var httpContext = httpContextAccessor.HttpContext;

        if (refreshTokenEntity == null || refreshTokenEntity.Revoked.HasValue)
        {
            var reason = refreshTokenEntity?.RevocationReason.ToString() ?? "Unknown reason";
            Log.Warning($"Refresh token is not valid or already revoked. Revocation Reason: {reason}, Token ID: {refreshTokenEntity?.Id}");

            httpContext?.Response.Redirect("/Account/Logout");

            return;
        }

        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        refreshTokenEntity.Revoked = DateTime.UtcNow;
        refreshTokenEntity.RevokedByIp = ipAddress;
        refreshTokenEntity.RevocationReason = revocationReason;

        context.Update(refreshTokenEntity);

        await context.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason)
    {
        var now = DateTime.UtcNow;
        var refreshTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.Revoked == null && rt.Expires > now)
            .ToListAsync();

        if (refreshTokens.Count > 0)
        {
            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.Revoked = now;
                refreshToken.RevokedByIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
                refreshToken.RevocationReason = revocationReason;
            }

            context.RefreshTokens.UpdateRange(refreshTokens);
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

        if (expiredTokens.Count > 0)
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
}
