// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class RsaKeyProvider(ILogger<RsaKeyProvider> logger) : IRsaKeyProvider
{
    private RSA? _rsa;

    public RSA? GetRsaPublicKey()
    {
        if (_rsa != null)
        {
            return _rsa;
        }

        try
        {
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");

            if (File.Exists(publicKeyPath))
            {
                var publicKey = File.ReadAllBytes(publicKeyPath);

                _rsa = RSA.Create();
                _rsa.ImportRSAPublicKey(publicKey, out _);
            }
            else
            {
                logger.LogWarning("Public key file not found at path: {PublicKeyPath}", publicKeyPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load RSA public key.");
        }

        return _rsa;
    }
}
