// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class CrlService(IOptions<CertificateOptions> options) : ICrlService
{
    private readonly CertificateOptions _settings = options.Value;
    
    private readonly List<(byte[] serialNumber, X509RevocationReason reason)> _revokedCertificates = [];
    private BigInteger _crlNumber = BigInteger.One;

    public void RevokeCertificate(byte[] serialNumber, X509RevocationReason reason)
    {
        _revokedCertificates.Add((serialNumber, reason));
    }

    public byte[] GenerateCrl()
    {
        var issuerCertificate = GetIssuerCertificate();
        var crlBuilder = new CertificateRevocationListBuilder();

        foreach (var (serialNumber, reason) in _revokedCertificates)
        {
            crlBuilder.AddEntry(serialNumber, DateTimeOffset.UtcNow, reason);
        }

        _crlNumber += 1;

        var nextUpdate = DateTimeOffset.UtcNow.AddDays(30);

        var crlData = crlBuilder.Build(issuerCertificate, _crlNumber, nextUpdate, HashAlgorithmName.SHA256);

        return crlData;
    }

    public void PublishCrl(byte[] crlData)
    {
    }

    private X509Certificate2 GetIssuerCertificate()
    {
        X509Certificate2? caCertificate = null;

        using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadOnly);
            
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, _settings.CommonName, false);

            foreach (var cert in certificates)
            {
                if (cert.HasPrivateKey)
                {
                    caCertificate = cert;
                    break;
                }
            }
        }

        if (caCertificate == null)
        {
            throw new InvalidOperationException($"CA certificate with CommonName '{_settings.CommonName}' not found.");
        }

        return caCertificate;
    }
}
