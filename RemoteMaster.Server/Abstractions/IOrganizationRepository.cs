// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.Abstractions;

public interface IOrganizationRepository : IRepository<Organization, Guid>
{
    Task<IEnumerable<Computer>> FindComputersAsync(Expression<Func<Computer, bool>> predicate);

    Task RemoveComputerAsync(Guid organizationId, Guid unitId, Guid computerId);

    Task<OrganizationalUnit?> GetOrganizationalUnitByIdAsync(Guid unitId);

    Task<Organization?> GetOrganizationByUnitIdAsync(Guid unitId);

    Task MoveComputerAsync(Guid sourceOrganizationId, Guid targetOrganizationId, Guid computerId, Guid sourceUnitId, Guid targetUnitId);

    Task<IEnumerable<CertificateRenewalTask>> GetAllCertificateRenewalTasksAsync();

    Task CreateCertificateRenewalTaskAsync(Guid organizationId, Guid computerId, DateTime plannedDate);

    Task DeleteCertificateRenewalTaskAsync(CertificateRenewalTask task);
}
