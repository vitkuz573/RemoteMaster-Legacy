// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Abstractions;

public interface IOrganizationalUnitService
{
    Task<List<string>> GetFullPathAsync(Guid organizationalUnitId);

    Task<string> AddOrUpdateOrganizationalUnitAsync(OrganizationalUnitDto dto);

    Task<string> DeleteOrganizationalUnitAsync(OrganizationalUnitDto organizationalUnit);

    Task<IEnumerable<OrganizationalUnitDto>> GetAllOrganizationalUnitsAsync();

    Task UpdateUserOrganizationalUnitsAsync(ApplicationUser user, List<Guid> unitIds);
}
