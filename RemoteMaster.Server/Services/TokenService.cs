// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class TokenService(IOptions<JwtOptions> options, ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : ITokenService
{
    private readonly JwtOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public async Task<string> GenerateAccessTokenAsync(string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email)
        };

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
            .SingleOrDefault(rt => rt.Token == refreshToken && rt.Revoked == null);

        if (tokenEntity == null)
        {
            return false;
        }

        return !tokenEntity.IsExpired;
    }

    public async Task<TokenResponseData?> RefreshAccessToken(string refreshToken)
    {
        var refreshTokenEntity = await context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.Revoked.HasValue);

        if (refreshTokenEntity == null || refreshTokenEntity.IsExpired)
        {
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

        context.RefreshTokens.Add(newRefreshTokenEntity);
        await context.SaveChangesAsync();

        refreshTokenEntity.Revoked = DateTime.UtcNow;
        refreshTokenEntity.RevokedByIp = ipAddress;
        refreshTokenEntity.RevocationReason = TokenRevocationReason.ReplacedDuringRefresh;
        refreshTokenEntity.ReplacedByToken = newRefreshTokenEntity;

        context.Update(refreshTokenEntity);

        await context.SaveChangesAsync();

        var user = await context.Users.FindAsync(refreshTokenEntity.UserId);

        if (user == null)
        {
            return null;
        }

        return new TokenResponseData
        {
            AccessToken = await GenerateAccessTokenAsync(user.Email),
            RefreshToken = newRefreshTokenEntity.Token,
        };
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, TokenRevocationReason revocationReason)
    {
        var refreshTokenEntity = await context.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == refreshToken);

        if (refreshTokenEntity == null || refreshTokenEntity.Revoked.HasValue)
        {
            throw new InvalidOperationException("Refresh token is not valid or already revoked.");
        }

        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

        refreshTokenEntity.Revoked = DateTime.UtcNow;
        refreshTokenEntity.RevokedByIp = ipAddress;
        refreshTokenEntity.RevocationReason = revocationReason;

        context.Update(refreshTokenEntity);
        await context.SaveChangesAsync();
    }
}
