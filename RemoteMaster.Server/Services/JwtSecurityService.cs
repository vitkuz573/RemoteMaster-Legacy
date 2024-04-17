// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;


public class JwtSecurityService : IJwtSecurityService
{
    private readonly JwtOptions _options;

    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    public JwtSecurityService(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        _privateKeyPath = Path.Combine(_options.KeysPath, "private_key.pem");
        _publicKeyPath = Path.Combine(_options.KeysPath, "public_key.pem");

        if (!Directory.Exists(_options.KeysPath))
        {
            Directory.CreateDirectory(_options.KeysPath);
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
