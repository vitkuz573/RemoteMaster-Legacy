// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.Services;

public class OrganizationService(IOrganizationRepository organizationRepository) : IOrganizationService
{
    public async Task<IEnumerable<Organization>> GetAllOrganizationsAsync()
    {
        return await organizationRepository.GetAllAsync();
    }
}
