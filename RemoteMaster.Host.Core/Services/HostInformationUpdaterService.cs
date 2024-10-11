// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Services;

public class HostInformationUpdaterService(IHostConfigurationService hostConfigurationService, IHostInformationService hostInformationService, IApiService apiService, ILogger<HostInformationUpdaterService> logger) : IHostInformationUpdaterService
{
    public async Task<bool> UpdateHostConfigurationAsync()
    {
        var hasChanges = false;

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();
        var hostInformation = hostInformationService.GetHostInformation();

        if (!hostConfiguration.Host.Equals(hostInformation))
        {
            if (!hostConfiguration.Host.MacAddress.Equals(hostInformation.MacAddress))
            {
                logger.LogInformation("MAC address has changed, which might indicate restoration from backup.");
            }

            hostConfiguration.Host = hostInformation;

            logger.LogInformation("Host details were either missing or have been updated.");

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
                logger.LogError(ex, "Error saving updated configuration.");

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

                    logger.LogInformation("HostMoveRequest applied: Organization changed to {Organization} and Organizational Unit changed to {OrganizationalUnit}.", hostMoveRequest.Organization, string.Join("/", hostMoveRequest.OrganizationalUnit));

                    var acknowledgeResult = await apiService.AcknowledgeMoveRequestAsync(hostConfiguration.Host.MacAddress);

                    if (acknowledgeResult)
                    {
                        logger.LogInformation("Host move request acknowledged");
                    }
                    else
                    {
                        logger.LogWarning("Failed to acknowledge host move request");
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
