// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class TokenService(IOptions<JwtOptions> options, ApplicationDbContext context) : ITokenService
{
    private readonly JwtOptions _options = options.Value ?? throw new ArgumentNullException(nameof(options));

    public string GenerateAccessToken(string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email)
        };

        var privateKey = File.ReadAllText(_options.PrivateKeyPath);

#pragma warning disable CA2000
        var rsa = RSA.Create();
#pragma warning restore CA2000
        rsa.ImportFromPem(privateKey.ToCharArray());

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
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
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        context.RefreshTokens.Add(refreshToken);
        context.SaveChanges();

        return refreshToken.Token;
    }

    public async Task<(string? AccessToken, string? RefreshToken)> RefreshTokensAsync(string oldRefreshToken, string ipAddress)
    {
        var oldRefreshTokenEntity = await context.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == oldRefreshToken && rt.Revoked == null && rt.Expires > DateTime.UtcNow);

        if (oldRefreshTokenEntity == null)
        {
            return (null, null);
        }

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = oldRefreshTokenEntity.UserId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        context.RefreshTokens.Add(newRefreshTokenEntity);
        await context.SaveChangesAsync();

        oldRefreshTokenEntity.Revoked = DateTime.UtcNow;
        oldRefreshTokenEntity.RevokedByIp = ipAddress;
        oldRefreshTokenEntity.ReplacedByToken = newRefreshTokenEntity.Token;

        await context.SaveChangesAsync();

        var user = await context.Users.FindAsync(oldRefreshTokenEntity.UserId);

        if (user == null)
        {
            return (null, null);
        }

        var newAccessToken = GenerateAccessToken(user.Email);

        return (newAccessToken, newRefreshTokenEntity.Token);
    }

    public bool RequiresTokenUpdate(string accessToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            if (tokenHandler.ReadToken(accessToken) is JwtSecurityToken jsonToken)
            {
                var expDate = jsonToken.ValidTo.ToUniversalTime();
                var currentDate = DateTime.UtcNow;

                return (expDate - currentDate).TotalMinutes <= 5;
            }
        }
        catch (ArgumentException ex)
        {
            Log.Error(ex, "An error occurred while reading JWT token: Invalid token format.");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unexpected error occurred while processing JWT token.");

            return true;
        }

        Log.Warning("The token provided is not a valid JWT token.");

        return true;
    }
}
