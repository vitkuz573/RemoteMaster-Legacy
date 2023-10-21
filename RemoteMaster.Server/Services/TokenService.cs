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
using RemoteMaster.Server.Areas.Identity.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class TokenService : ITokenService
{
    private readonly TokenServiceOptions _options;
    private readonly IdentityDataContext _context;

    public TokenService(IOptions<TokenServiceOptions> options, IdentityDataContext context)
    {
        _options = options?.Value;
        _context = context;
    }

    public string GenerateAccessToken(string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, email)
        };

        var privateKey = File.ReadAllText(_options.PrivateKeyPath);

#pragma warning disable CA2000
        var ecdsa = ECDsa.Create();
#pragma warning restore CA2000
        ecdsa.ImportFromPem(privateKey.ToCharArray());

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(ecdsa), SecurityAlgorithms.EcdsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    public async Task<bool> SaveRefreshToken(string email, string refreshToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return false;
        }

        var token = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(token);

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<string> RefreshAccessToken(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.Token == refreshToken && t.ExpiryDate > DateTime.UtcNow) ?? throw new Exception("Invalid refresh token or token has expired.");
        _context.RefreshTokens.Remove(storedToken);
        await _context.SaveChangesAsync();

        return GenerateAccessToken(storedToken.User.Email);
    }

    public async Task<bool> RevokeRefreshToken(string refreshToken)
    {
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (storedToken == null)
        {
            return false;
        }

        _context.RefreshTokens.Remove(storedToken);

        return await _context.SaveChangesAsync() > 0;
    }
}
