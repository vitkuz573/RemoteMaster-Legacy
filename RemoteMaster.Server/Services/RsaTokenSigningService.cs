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
using Serilog;

namespace RemoteMaster.Server.Services;

public class RsaTokenSigningService : ITokenSigningService, IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly JwtOptions _options;
    private readonly RSA _signingRsa;
    private readonly RSA _validationRsa;

    public RsaTokenSigningService(IFileSystem fileSystem, IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _fileSystem = fileSystem;
        _options = options.Value;

        _signingRsa = RSA.Create();
        _validationRsa = RSA.Create();

        InitializeSigningRsaKey();
        InitializeValidationRsaKey();
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
            Log.Error("Failed to load the private key for signing: {Message}", ex.Message);
            throw;
        }
    }

    private void InitializeValidationRsaKey()
    {
        try
        {
            var publicKeyPath = _fileSystem.Path.Combine(_options.KeysDirectory, "public_key.der");
            var publicKeyBytes = _fileSystem.File.ReadAllBytes(publicKeyPath);

            _validationRsa.ImportRSAPublicKey(publicKeyBytes, out _);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load the public key for validation: {Message}", ex.Message);

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
            Log.Error("Failed to sign the token: {Message}.", ex.Message);

            return Result.Fail<string>("Failed to sign the token.");
        }
        catch (Exception ex)
        {
            Log.Error("Unknown error occurred during token generation: {Message}.", ex.Message);

            return Result.Fail<string>("Unknown error occurred during token generation.");
        }
    }

    public Result ValidateToken(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return Result.Fail("Access token cannot be null or empty.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(_validationRsa),
                ValidateIssuer = true,
                ValidIssuer = "RemoteMaster Server",
                ValidateAudience = true,
                ValidAudience = "RMServiceAPI",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            return validatedToken == null ? Result.Fail("Invalid token.") : Result.Ok();
        }
        catch (SecurityTokenException ex)
        {
            Log.Error("Token validation failed: {Message}.", ex.Message);

            return Result.Fail("Token validation failed.").WithError(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Unknown error occurred during token validation: {Message}.", ex.Message);

            return Result.Fail("Unknown error occurred during token validation.");
        }
    }

    public void Dispose()
    {
        _signingRsa.Dispose();
        _validationRsa.Dispose();
    }
}
