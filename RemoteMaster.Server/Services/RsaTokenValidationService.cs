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

public class RsaTokenValidationService : ITokenValidationService
{
    private readonly IFileSystem _fileSystem;
    private readonly JwtOptions _options;
    private readonly ILogger<RsaTokenValidationService> _logger;
    
    private readonly RSA _validationRsa;

    public RsaTokenValidationService(IFileSystem fileSystem, IOptions<JwtOptions> options, ILogger<RsaTokenValidationService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _fileSystem = fileSystem;
        _options = options.Value;
        _logger = logger;

        _validationRsa = RSA.Create();

        InitializeValidationRsaKey();
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
            _logger.LogError("Failed to load the public key for validation: {Message}", ex.Message);
            throw;
        }
    }

    public Result ValidateToken(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return Result.Fail("Access token cannot be null or empty.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
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

        try
        {
            tokenHandler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            return validatedToken == null ? Result.Fail("Invalid token.") : Result.Ok();
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogError("Token validation failed: {Message}.", ex.Message);

            return Result.Fail("Token validation failed.").WithError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unknown error occurred during token validation: {Message}.", ex.Message);

            return Result.Fail("Unknown error occurred during token validation.");
        }
    }

    public void Dispose()
    {
        _validationRsa.Dispose();
    }
}
