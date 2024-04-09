// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateManagementService(IHostConfigurationService hostConfigurationService, IHostLifecycleService hostLifecycleService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        if (hostConfiguration != null && IsCertificateValid())
        {
            await hostLifecycleService.RenewCertificateAsync(hostConfiguration);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static bool IsCertificateValid()
    {
        X509Certificate2? сertificate = null;

        using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);

            foreach (var cert in certificates)
            {
                if (cert.HasPrivateKey)
                {
                    сertificate = cert;
                    break;
                }
            }
        }

        if (сertificate == null)
        {
            return false;
        }

        var currentTime = DateTime.Now;

        return (currentTime >= сertificate.NotBefore) && (currentTime <= сertificate.NotAfter);
    }
}
