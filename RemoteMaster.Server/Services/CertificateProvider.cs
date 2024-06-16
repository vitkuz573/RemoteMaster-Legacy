// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class CertificateProvider(IOptions<CertificateOptions> options) : ICertificateProvider
{
    private readonly CertificateOptions _settings = options.Value;

    public X509Certificate2 GetIssuerCertificate()
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
