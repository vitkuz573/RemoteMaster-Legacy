// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using Host = RemoteMaster.Server.Aggregates.OrganizationAggregate.Host;

namespace RemoteMaster.Server.Abstractions;

public interface IOrganizationRepository : IRepository<Organization, Guid>
{
    Task<IEnumerable<Host>> FindHostsAsync(Expression<Func<Host, bool>> predicate);

    Task RemoveHostAsync(Guid organizationId, Guid unitId, Guid hostId);

    Task<OrganizationalUnit?> GetOrganizationalUnitByIdAsync(Guid unitId);

    Task<Organization?> GetOrganizationByUnitIdAsync(Guid unitId);

    Task MoveHostAsync(Guid sourceOrganizationId, Guid targetOrganizationId, Guid hostId, Guid sourceUnitId, Guid targetUnitId);

    Task<IEnumerable<Organization>> GetOrganizationsWithAccessibleUnitsAsync(IEnumerable<Guid> organizationIds, IEnumerable<Guid> organizationalUnitIds);
}
