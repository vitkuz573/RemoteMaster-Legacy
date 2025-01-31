// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IdentityModel.Tokens.Jwt;
using System.IO.Abstractions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentResults;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Services;

public class RsaTokenSigningService(IFileSystem fileSystem, IOptions<JwtOptions> options, ILogger<RsaTokenSigningService> logger) : ITokenSigningService
{
    private const string Issuer = "RemoteMaster Server";
    private const string Audience = "RMServiceAPI";
    private const int TokenLifetimeMinutes = 15;
    private readonly JwtOptions _options = options.Value;
    private RSA? _signingRsa;

    private RSA GetSigningRsa()
    {
        if (_signingRsa != null)
        {
            return _signingRsa;
        }

        try
        {
            var privateKeyPath = fileSystem.Path.Combine(_options.KeysDirectory, "private_key.der");
            var privateKeyBytes = fileSystem.File.ReadAllBytes(privateKeyPath);

            _signingRsa = RSA.Create();
            _signingRsa.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(_options.KeyPassword), privateKeyBytes, out _);
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError("Private key file not found: {Message}", ex.Message);
            throw;
        }
        catch (CryptographicException ex)
        {
            logger.LogError("Failed to decrypt the private key: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError("Unknown error occurred during private key initialization: {Message}", ex.Message);
            throw;
        }

        return _signingRsa;
    }

    public Result<string> GenerateAccessToken(List<Claim> claims)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = Audience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(TokenLifetimeMinutes),
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(GetSigningRsa()), SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Result.Ok(tokenHandler.WriteToken(token));
        }
        catch (CryptographicException ex)
        {
            logger.LogError("Failed to sign the token: {Message}", ex.Message);

            return Result.Fail<string>("Failed to sign the token.");
        }
        catch (Exception ex)
        {
            logger.LogError("Unknown error occurred during token generation: {Message}", ex.Message);

            return Result.Fail<string>("Unknown error occurred during token generation.");
        }
    }

    public void Dispose()
    {
        _signingRsa?.Dispose();
    }
}
