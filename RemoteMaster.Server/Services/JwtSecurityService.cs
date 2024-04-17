// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;


public class JwtSecurityService : IJwtSecurityService
{
    private static readonly string _programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    private readonly string _destDirectory = Path.Combine(_programDataPath, "RemoteMaster", "Security", "JWT");

    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    public JwtSecurityService()
    {
        _privateKeyPath = Path.Combine(_destDirectory, "private_key.pem");
        _publicKeyPath = Path.Combine(_destDirectory, "public_key.pem");

        if (!Directory.Exists(_destDirectory))
        {
            Directory.CreateDirectory(_destDirectory);
        }
    }

    public void EnsureKeysExist()
    {
        if (!File.Exists(_privateKeyPath) || !File.Exists(_publicKeyPath))
        {
            using var rsa = RSA.Create(4096);

            File.WriteAllText(_privateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
            File.WriteAllText(_publicKeyPath, rsa.ExportRSAPublicKeyPem());
        }
    }
}
