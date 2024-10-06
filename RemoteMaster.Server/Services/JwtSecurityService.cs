// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;
using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;
using Serilog;

namespace RemoteMaster.Server.Services;

public class JwtSecurityService : IJwtSecurityService
{
    private readonly JwtOptions _options;
    private readonly IFileSystem _fileSystem;

    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    public JwtSecurityService(IOptions<JwtOptions> options, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fileSystem);

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _fileSystem = fileSystem;

        _privateKeyPath = _fileSystem.Path.Combine(_options.KeysDirectory, "private_key.der");
        _publicKeyPath = _fileSystem.Path.Combine(_options.KeysDirectory, "public_key.der");

        if (!_fileSystem.Directory.Exists(_options.KeysDirectory))
        {
            _fileSystem.Directory.CreateDirectory(_options.KeysDirectory);
        }
    }

    public async Task<Result<byte[]?>> GetPublicKeyAsync()
    {
        try
        {
            if (!_fileSystem.File.Exists(_publicKeyPath))
            {
                return Result.Fail<byte[]?>("Public key file does not exist.");
            }

            var publicKey = await _fileSystem.File.ReadAllBytesAsync(_publicKeyPath);

            return Result.Ok<byte[]?>(publicKey);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while reading public key file.");

            return Result.Fail<byte[]?>("Error while reading public key file.").WithError(ex.Message);
        }
    }

    public async Task<Result> EnsureKeysExistAsync()
    {
        Log.Debug("Checking existence of JWT keys.");

        if (!_fileSystem.File.Exists(_privateKeyPath) || !_fileSystem.File.Exists(_publicKeyPath))
        {
            Log.Information("JWT keys not found. Generating new keys.");

            try
            {
                using var rsa = RSA.Create(_options.KeySize);
                var passwordBytes = Encoding.UTF8.GetBytes(_options.KeyPassword);
                var encryptionAlgorithm = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100000);

                await _fileSystem.File.WriteAllBytesAsync(_privateKeyPath, rsa.ExportEncryptedPkcs8PrivateKey(passwordBytes, encryptionAlgorithm));
                await _fileSystem.File.WriteAllBytesAsync(_publicKeyPath, rsa.ExportRSAPublicKey());

                Log.Information("JWT keys generated and saved successfully.");

                return Result.Ok();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to generate JWT keys.");

                return Result.Fail("Failed to generate JWT keys.").WithError(ex.Message);
            }
        }

        Log.Information("JWT keys already exist.");

        return Result.Ok();
    }
}
