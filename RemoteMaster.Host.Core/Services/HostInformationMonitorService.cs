// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostInformationMonitorService(IServerHubService serverHubService, IHostConfigurationService hostConfigurationService, IHostInformationService hostInformationService) : IHostInformationMonitorService
{
    public async Task<bool> UpdateHostConfigurationAsync()
    {
        var hasChanges = false;

        HostConfiguration hostConfiguration;

        try
        {
            hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
        {
            Log.Error(ex, "Error loading configuration.");

            return false;
        }

        var hostInformation = hostInformationService.GetHostInformation();

        if (hostConfiguration.Host == null || !hostConfiguration.Host.Equals(hostInformation))
        {
            hostConfiguration.Host = hostInformation;

            Log.Information("Host details were either missing or have been updated.");

            hasChanges = true;
        }

        try
        {
            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving updated configuration.");

            return false;
        }

        try
        {
            await serverHubService.ConnectAsync(hostConfiguration.Server);
            var newOrganizationalUnits = await serverHubService.GetNewOrganizationalUnitIfChangeRequested(hostConfiguration.Host.MacAddress);

            if (newOrganizationalUnits.Length > 0)
            {
                hostConfiguration.Subject.OrganizationalUnit = newOrganizationalUnits;

                await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

                Log.Information("Organizational unit for this device was updated based on the organizational unit change request.");

                await serverHubService.AcknowledgeOrganizationalUnitChange(hostConfiguration.Host.MacAddress);

                hasChanges = true;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing organizational unit change requests.");
        }

        return hasChanges;
    }

    public bool CheckCertificateExpiration()
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
