// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateManagementService(ICertificateService certificateService, IFileSystem fileSystem, IApplicationPathProvider applicationPathProvider, IHostConfigurationService hostConfigurationService, IHostLifecycleService hostLifecycleService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hostConfiguration = await hostConfigurationService.LoadAsync();

        if (!IsCertificateValid())
        {
            var organizationAddress = await hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration.Subject.Organization);

            await certificateService.IssueCertificateAsync(hostConfiguration, organizationAddress);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private bool IsCertificateValid()
    {
        X509Certificate2? certificate = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);

            foreach (var cert in certificates.Where(cert => cert.HasPrivateKey))
            {
                certificate = cert;
                break;
            }
        }
        else
        {
            var certPath = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, "device_certificate.pem");
            var keyPath = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, "device_privatekey.pem");

            if (!fileSystem.File.Exists(certPath))
            {
                return false;
            }

            if (!fileSystem.File.Exists(keyPath))
            {
                return false;
            }

            var keyPem = fileSystem.File.ReadAllText(keyPath);

            var loadedCertificate = X509CertificateLoader.LoadCertificateFromFile(certPath);

#pragma warning disable CA2000
            var rsa = RSA.Create();
#pragma warning restore CA2000
            rsa.ImportFromPem(keyPem);

            var loadedCertificateWithPrivateKey = loadedCertificate.CopyWithPrivateKey(rsa);

            if (loadedCertificateWithPrivateKey.HasPrivateKey)
            {
                certificate = loadedCertificateWithPrivateKey;
            }
        }

        if (certificate == null)
        {
            return false;
        }

        var currentTime = DateTime.Now;

        return currentTime >= certificate.NotBefore && currentTime <= certificate.NotAfter;
    }
}
