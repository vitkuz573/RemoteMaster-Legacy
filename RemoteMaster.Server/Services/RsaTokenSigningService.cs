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

public class RsaTokenSigningService : ITokenSigningService
{
    private readonly IFileSystem _fileSystem;
    private readonly JwtOptions _options;
    private readonly ILogger<RsaTokenSigningService> _logger;

    private readonly RSA _signingRsa;

    public RsaTokenSigningService(IFileSystem fileSystem, IOptions<JwtOptions> options, ILogger<RsaTokenSigningService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _fileSystem = fileSystem;
        _options = options.Value;
        _logger = logger;

        _signingRsa = RSA.Create();

        InitializeSigningRsaKey();
    }

    private void InitializeSigningRsaKey()
    {
        try
        {
            var privateKeyPath = _fileSystem.Path.Combine(_options.KeysDirectory, "private_key.der");
            var privateKeyBytes = _fileSystem.File.ReadAllBytes(privateKeyPath);

            _signingRsa.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(_options.KeyPassword), privateKeyBytes, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load the private key for signing: {Message}", ex.Message);
            throw;
        }
    }

    public Result<string> GenerateAccessToken(List<Claim> claims)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = "RemoteMaster Server",
                Audience = "RMServiceAPI",
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(_signingRsa), SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Result.Ok(tokenHandler.WriteToken(token));
        }
        catch (CryptographicException ex)
        {
            _logger.LogError("Failed to sign the token: {Message}.", ex.Message);

            return Result.Fail<string>("Failed to sign the token.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Unknown error occurred during token generation: {Message}.", ex.Message);

            return Result.Fail<string>("Unknown error occurred during token generation.");
        }
    }

    public void Dispose()
    {
        _signingRsa.Dispose();
    }
}
