// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class OrganizationalUnitService(IOrganizationalUnitRepository organizationalUnitRepository) : IOrganizationalUnitService
{
    public async Task<string[]> GetFullPathAsync(Guid organizationalUnitId)
    {
        var path = new List<string>();
        var unit = await organizationalUnitRepository.GetByIdAsync(organizationalUnitId);

        while (unit != null)
        {
            path.Insert(0, unit.Name);

            if (unit.ParentId == null)
            {
                break;
            }

            unit = await organizationalUnitRepository.GetByIdAsync(unit.ParentId.Value);
        }

        return [.. path];
    }
}
