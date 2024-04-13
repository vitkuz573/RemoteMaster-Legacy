// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class JwtSecurityService : IJwtSecurityService
{
    private readonly string _destDirectory = @"C:\ProgramData\RemoteMaster\Security\JWT";
    
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    public JwtSecurityService()
    {
        _privateKeyPath = Path.Combine(_destDirectory, "private_key.pem");
        _publicKeyPath = Path.Combine(_destDirectory, "public_key.pem");
        
        EnsureKeysDirectoryExists();
    }

    public void EnsureKeysExist()
    {
        if (!File.Exists(_privateKeyPath) || !File.Exists(_publicKeyPath))
        {
            GenerateRsaKeys();
        }
        else
        {
            Log.Information("RSA keys already exist and will not be regenerated.");
        }
    }

    private void GenerateRsaKeys()
    {
        using (var rsa = RSA.Create(4096))
        {
            ExportPrivateKey(rsa);
            ExportPublicKey(rsa);
        }

        Log.Information($"RSA keys successfully generated in '{_destDirectory}'");
    }

    private void EnsureKeysDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_destDirectory))
            {
                Directory.CreateDirectory(_destDirectory);
                Log.Information($"Created directory: {_destDirectory}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to create directory: {_destDirectory}");
        }
    }

    private void ExportPrivateKey(RSA rsa)
    {
        var privateKeyBytes = rsa.ExportRSAPrivateKey();
        var privateKeyPem = ConvertToPem(privateKeyBytes, "RSA PRIVATE KEY");

        File.WriteAllText(_privateKeyPath, privateKeyPem);
        Log.Information($"Private key saved to: {_privateKeyPath}");
    }

    private void ExportPublicKey(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportRSAPublicKey();
        var publicKeyPem = ConvertToPem(publicKeyBytes, "PUBLIC KEY");

        File.WriteAllText(_publicKeyPath, publicKeyPem);
        Log.Information($"Public key saved to: {_publicKeyPath}");
    }

    private static string ConvertToPem(byte[] keyBytes, string title)
    {
        var base64 = Convert.ToBase64String(keyBytes);

        return $"-----BEGIN {title}-----\n{base64}\n-----END {title}-----";
    }
}
