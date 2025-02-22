// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Abstractions;

public interface IOrganizationService
{
    Task<IEnumerable<OrganizationDto>> GetAllOrganizationsAsync();

    Task<OrganizationDto?> GetOrganization(string organizationName);

    Task<OrganizationDto?> GetOrganizationById(Guid organizationId);

    Task<string> AddOrUpdateOrganizationAsync(OrganizationDto dto);

    Task<string> DeleteOrganizationAsync(string organizationName);

    Task UpdateUserOrganizationsAsync(ApplicationUser user, List<Guid> organizationIds);

    Task<IEnumerable<Organization>> GetOrganizationsWithAccessibleUnitsAsync(string userId);

    Task RemoveHostAsync(Guid organizationId, Guid organizationalUnitId, Guid hostId);
}
