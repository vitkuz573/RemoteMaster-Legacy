// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.DTOs;

namespace RemoteMaster.Server.Abstractions;

public interface IOrganizationalUnitService
{
    Task<string[]> GetFullPathAsync(Guid organizationalUnitId);

    Task<string> AddOrUpdateOrganizationalUnitAsync(OrganizationalUnitDto dto);

    Task<string> DeleteOrganizationalUnitAsync(OrganizationalUnit organizationalUnit);

    Task<IEnumerable<OrganizationalUnit>> GetAllOrganizationalUnitsAsync();

    Task UpdateUserOrganizationalUnitsAsync(ApplicationUser user, List<Guid> selectedUnitIds);
}
