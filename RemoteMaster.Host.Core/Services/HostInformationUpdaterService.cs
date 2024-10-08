// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
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

        if (!hostConfiguration.Host.Equals(hostInformation))
        {
            if (!hostConfiguration.Host.MacAddress.Equals(hostInformation.MacAddress))
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
                var isOrganizationChanged = hostConfiguration.Subject.Organization != hostMoveRequest.Organization;
                var isOrganizationalUnitChanged = !hostConfiguration.Subject.OrganizationalUnit.SequenceEqual(hostMoveRequest.OrganizationalUnit);

                if (isOrganizationChanged || isOrganizationalUnitChanged)
                {
                    hostConfiguration.Subject = new SubjectDto(hostMoveRequest.Organization, hostMoveRequest.OrganizationalUnit);

                    await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

                    Log.Information("HostMoveRequest applied: Organization changed to {Organization} and Organizational Unit changed to {OrganizationalUnit}.", hostMoveRequest.Organization, string.Join("/", hostMoveRequest.OrganizationalUnit));

                    var acknowledgeResult = await apiService.AcknowledgeMoveRequestAsync(hostConfiguration.Host.MacAddress);

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
}
