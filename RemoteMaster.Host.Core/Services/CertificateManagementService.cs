// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateManagementService(IHostConfigurationService hostConfigurationService, IHostLifecycleService hostLifecycleService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        if (hostConfiguration != null && CheckCertificateExpiration())
        {
            await hostLifecycleService.RenewCertificateAsync(hostConfiguration);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static bool CheckCertificateExpiration()
    {
        X509Certificate2? httpsCertificate = null;

        using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, Environment.MachineName, false);

            foreach (var cert in certificates)
            {
                if (cert.HasPrivateKey)
                {
                    httpsCertificate = cert;
                    break;
                }
            }
        }

        return DateTime.Now > httpsCertificate?.NotAfter;
    }
}
