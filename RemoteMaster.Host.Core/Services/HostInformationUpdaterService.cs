// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostInformationUpdaterService(IHostConfigurationService hostConfigurationService, IHostInformationService hostInformationService, IApiService apiService) : IHostInformationUpdaterService
{
    public async Task<bool> UpdateHostConfigurationAsync()
    {
        var hasChanges = false;

        HostConfiguration hostConfiguration;

        try
        {
            hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
        {
            Log.Error(ex, "Error loading configuration.");

            return false;
        }

        var hostInformation = hostInformationService.GetHostInformation();

        if (hostConfiguration.Host == null || !hostConfiguration.Host.Equals(hostInformation))
        {
            if (hostConfiguration.Host != null && !hostConfiguration.Host.MacAddress.GetAddressBytes().SequenceEqual(hostInformation.MacAddress.GetAddressBytes()))
            {
                Log.Information("MAC address has changed, which might indicate restoration from backup.");
            }

            hostConfiguration.Host = hostInformation;

            Log.Information("Host details were either missing or have been updated.");

            hasChanges = true;
        }

        if (hasChanges)
        {
            try
            {
                await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving updated configuration.");

                return false;
            }
        }

        try
        {
            var hostMoveRequest = await apiService.GetHostMoveRequestAsync(hostConfiguration.Host.MacAddress);

            if (hostMoveRequest != null)
            {
                var isOrganizationChanged = hostConfiguration.Subject.Organization != hostMoveRequest.NewOrganization;
                var isOrganizationalUnitChanged =
                    !hostConfiguration.Subject.OrganizationalUnit.SequenceEqual(hostMoveRequest.NewOrganizationalUnit);

                if (isOrganizationChanged || isOrganizationalUnitChanged)
                {
                    hostConfiguration.Subject.Organization = hostMoveRequest.NewOrganization;
                    hostConfiguration.Subject.OrganizationalUnit = hostMoveRequest.NewOrganizationalUnit;

                    await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

                    Log.Information(
                        "HostMoveRequest applied: Organization changed to {Organization} and Organizational Unit changed to {OrganizationalUnit}.",
                        hostMoveRequest.NewOrganization, string.Join("/", hostMoveRequest.NewOrganizationalUnit));

                    var acknowledgeResult =
                        await apiService.AcknowledgeMoveRequestAsync(hostConfiguration.Host.MacAddress);

                    if (acknowledgeResult)
                    {
                        Log.Information("Host move request acknowledged");
                    }
                    else
                    {
                        Log.Warning("Failed to acknowledge host move request");
                    }

                    hasChanges = true;
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return hasChanges;
    }

    public bool CheckCertificateExpiration()
    {
        X509Certificate2? certificate = null;

        using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);

            foreach (var cert in certificates.Where(cert => cert.HasPrivateKey))
            {
                certificate = cert;
                break;
            }
        }

        return certificate == null || DateTime.Now > certificate.NotAfter;
    }
}
