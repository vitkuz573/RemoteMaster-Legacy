// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Host.Core.Hubs;

public class CertificateHub(ICertificateService certificateService, IHostConfigurationService hostConfigurationService, IHostLifecycleService hostLifecycleService, ICertificateStoreService certificateStoreService) : Hub<ICertificateClient>
{
    [Authorize(Policy = "RenewCertificatePolicy")]
    public async Task RenewCertificate()
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();
        var organizationAddress = await hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration.Subject.Organization);
        
        await certificateService.IssueCertificateAsync(hostConfiguration, organizationAddress);
    }

    [Authorize(Policy = "GetCertificateSerialNumberPolicy")]
    public async Task GetCertificateSerialNumber()
    {
        var certificates = certificateStoreService.GetCertificates(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, Dns.GetHostName());
        var certificate = certificates.FirstOrDefault(c => c.HasPrivateKey);
        var serialNumber =  certificate?.GetSerialNumberString();

        await Clients.Caller.ReceiveCertificateSerialNumber(serialNumber);
    }
}
