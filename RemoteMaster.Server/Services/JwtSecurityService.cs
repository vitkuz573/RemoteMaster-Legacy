// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Server.Abstractions;

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
            GenerateAndSaveRsaKeys();
        }
    }

    private void GenerateAndSaveRsaKeys()
    {
        using var rsa = RSA.Create(4096);

        ExportPrivateKey(rsa, _privateKeyPath);
        ExportPublicKey(rsa, _publicKeyPath);
    }

    private void EnsureKeysDirectoryExists()
    {
        if (!Directory.Exists(_destDirectory))
        {
            Directory.CreateDirectory(_destDirectory);
        }
    }

    private static void ExportPrivateKey(RSA rsa, string filePath)
    {
        var privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();

        File.WriteAllText(filePath, privateKeyPem);
    }

    private static void ExportPublicKey(RSA rsa, string filePath)
    {
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();

        File.WriteAllText(filePath, publicKeyPem);
    }
}
