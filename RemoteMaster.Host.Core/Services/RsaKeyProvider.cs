// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class RsaKeyProvider(IFileSystem fileSystem, IApplicationPathProvider applicationPathProvider, ILogger<RsaKeyProvider> logger) : IRsaKeyProvider
{
    private RSA? _rsa;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<RSA?> GetRsaPublicKeyAsync()
    {
        if (_rsa != null)
        {
            return _rsa;
        }

        await _semaphore.WaitAsync();

        try
        {
            if (_rsa != null)
            {
                return _rsa;
            }

            var publicKeyPath = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, "JWT", "public_key.der");

            if (fileSystem.File.Exists(publicKeyPath))
            {
                var publicKey = await fileSystem.File.ReadAllBytesAsync(publicKeyPath);

                if (publicKey.Length == 0)
                {
                    logger.LogError("Public key file is empty.");

                    return null;
                }

                var rsa = RSA.Create();
                rsa.ImportRSAPublicKey(publicKey, out _);

                _rsa = rsa;
            }
            else
            {
                logger.LogWarning("Public key file not found at path: {PublicKeyPath}", publicKeyPath);
            }

            return _rsa;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load RSA public key.");

            _rsa = null;

            return _rsa;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
