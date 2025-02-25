// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IdentityModel.Tokens.Jwt;
using System.IO.Abstractions;
using System.Security.Cryptography;
using FluentResults;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Services;

public class RsaTokenValidationService(IFileSystem fileSystem, IOptions<JwtOptions> options, ILogger<RsaTokenValidationService> logger) : ITokenValidationService
{
    private readonly JwtOptions _options = options.Value;
    private RSA? _validationRsa;

    private async Task<RSA> GetValidationRsaAsync()
    {
        if (_validationRsa == null)
        {
            try
            {
                var publicKeyPath = fileSystem.Path.Combine(_options.KeysDirectory, "public_key.der");
                var publicKeyBytes = await fileSystem.File.ReadAllBytesAsync(publicKeyPath);

                _validationRsa = RSA.Create();
                _validationRsa.ImportRSAPublicKey(publicKeyBytes, out _);
            }
            catch (FileNotFoundException ex)
            {
                logger.LogError("Public key file not found: {Message}", ex.Message);
                throw;
            }
            catch (CryptographicException ex)
            {
                logger.LogError("Failed to import the public key: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError("Unknown error occurred during public key initialization: {Message}", ex.Message);
                throw;
            }
        }

        return _validationRsa;
    }

    public async Task<Result> ValidateTokenAsync(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return Result.Fail("Access token cannot be null or empty.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(await GetValidationRsaAsync()),
            ValidateIssuer = true,
            ValidIssuer = "RemoteMaster Server",
            ValidateAudience = true,
            ValidAudience = "RMServiceAPI",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            return validatedToken == null ? Result.Fail("Invalid token.") : Result.Ok();
        }
        catch (SecurityTokenException ex)
        {
            logger.LogError("Token validation failed: {Message}.", ex.Message);

            return Result.Fail("Token validation failed.").WithError(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError("Unknown error occurred during token validation: {Message}.", ex.Message);

            return Result.Fail("Unknown error occurred during token validation.");
        }
    }

    public void Dispose()
    {
        _validationRsa?.Dispose();
    }
}
