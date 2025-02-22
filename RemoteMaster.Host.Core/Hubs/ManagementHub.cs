// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Hubs;

public class ManagementHub(IHostLifecycleService hostLifecycleService, IHostConfigurationService hostConfigurationService, ICertificateService certificateService) : Hub
{
    [Authorize(Policy = "MoveHostPolicy")]
    public async Task MoveHost(HostMoveRequestDto hostMoveRequest)
    {
        ArgumentNullException.ThrowIfNull(hostMoveRequest);

        var hostConfiguration = await hostConfigurationService.LoadAsync();

        hostConfiguration.Subject = new SubjectDto(hostMoveRequest.Organization, hostMoveRequest.OrganizationalUnit);

        var organizationAddress = await hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration.Subject.Organization);

        await hostConfigurationService.SaveAsync(hostConfiguration);
        await certificateService.IssueCertificateAsync(hostConfiguration, organizationAddress);
    }
}
