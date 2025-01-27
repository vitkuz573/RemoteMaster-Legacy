// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.DomainEvents;
using Host = RemoteMaster.Server.Aggregates.OrganizationAggregate.Host;

namespace RemoteMaster.Server.Repositories;

public class OrganizationRepository(ApplicationDbContext context) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Hosts)
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Children)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Organization>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Hosts)
            .Where(o => ids.Contains(o.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<Organization>> GetAllAsync()
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Hosts)
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Children)
            .ToListAsync();
    }

    public async Task<IEnumerable<Organization>> FindAsync(Expression<Func<Organization, bool>> predicate)
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Hosts)
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(Organization entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await context.Organizations.AddAsync(entity);

        entity.AddDomainEvent(new OrganizationCreatedEvent(entity.Id, entity.Name, entity.Address));
    }

    public void Update(Organization entity)
    {
        context.Organizations.Update(entity);
    }

    public void Delete(Organization entity)
    {
        context.Organizations.Remove(entity);
    }

    public async Task<IEnumerable<Host>> FindHostsAsync(Expression<Func<Host, bool>> predicate)
    {
        return await context.Hosts
            .Include(h => h.Parent)
            .ThenInclude(ou => ou.Organization)
            .Where(predicate)
            .ToListAsync();
    }

    public async Task RemoveHostAsync(Guid organizationId, Guid unitId, Guid hostId)
    {
        var organization = await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Hosts)
            .FirstOrDefaultAsync(o => o.Id == organizationId) ?? throw new InvalidOperationException("Organization not found.");

        var unit = organization.OrganizationalUnits.FirstOrDefault(ou => ou.Id == unitId) ?? throw new InvalidOperationException("Organizational unit not found.");
        var host = unit.Hosts.FirstOrDefault(h => h.Id == hostId) ?? throw new InvalidOperationException("Host not found.");

        unit.RemoveHost(host.Id);
        context.Hosts.Remove(host);
    }

    public async Task<OrganizationalUnit?> GetOrganizationalUnitByIdAsync(Guid unitId)
    {
        return await context.Organizations
            .SelectMany(o => o.OrganizationalUnits)
            .Include(ou => ou.Hosts)
            .Include(ou => ou.UserOrganizationalUnits)
            .FirstOrDefaultAsync(ou => ou.Id == unitId);
    }

    public async Task<Organization?> GetOrganizationByUnitIdAsync(Guid unitId)
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Hosts)
            .FirstOrDefaultAsync(o => o.OrganizationalUnits.Any(ou => ou.Id == unitId));
    }

    public async Task MoveHostAsync(Guid sourceOrganizationId, Guid targetOrganizationId, Guid hostId, Guid sourceUnitId, Guid targetUnitId)
    {
        var sourceOrganization = await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Hosts)
            .FirstOrDefaultAsync(o => o.Id == sourceOrganizationId) ?? throw new InvalidOperationException("Source organization not found.");

        var targetOrganization = await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ThenInclude(ou => ou.Hosts)
            .FirstOrDefaultAsync(o => o.Id == targetOrganizationId) ?? throw new InvalidOperationException("Target organization not found.");

        var sourceUnit = sourceOrganization.OrganizationalUnits.FirstOrDefault(u => u.Id == sourceUnitId) ?? throw new InvalidOperationException("Source unit not found.");
        var host = sourceUnit.Hosts.FirstOrDefault(h => h.Id == hostId) ?? throw new InvalidOperationException("Host not found in the source unit.");
        var targetUnit = targetOrganization.OrganizationalUnits.FirstOrDefault(u => u.Id == targetUnitId) ?? throw new InvalidOperationException("Target unit not found.");

        sourceUnit.RemoveHost(host.Id);
        host.SetOrganizationalUnit(targetUnit.Id);
        targetUnit.AddExistingHost(host);

        context.Organizations.Update(sourceOrganization);
        context.Organizations.Update(targetOrganization);
    }

    public async Task<IEnumerable<Organization>> GetOrganizationsWithAccessibleUnitsAsync(IEnumerable<Guid> organizationIds, IEnumerable<Guid> organizationalUnitIds)
    {
        return await context.Organizations
            .Where(o => organizationIds.Contains(o.Id))
            .Include(o => o.OrganizationalUnits
                .Where(ou => organizationalUnitIds.Contains(ou.Id)))
            .ThenInclude(ou => ou.Hosts)
            .ToListAsync();
    }
}
