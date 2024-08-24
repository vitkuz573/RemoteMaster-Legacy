// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Abstractions;

public interface IOrganizationalUnitRepository : IRepository<OrganizationalUnit, Guid>
{
    Task<IEnumerable<Computer>> FindComputersAsync(Expression<Func<Computer, bool>> predicate);

    Task RemoveComputerAsync(OrganizationalUnit organizationalUnit, Computer computer);
}
