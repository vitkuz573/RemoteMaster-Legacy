// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class OrganizationalUnitRepository(ApplicationDbContext context) : IOrganizationalUnitRepository
{
    public async Task<OrganizationalUnit?> GetByIdAsync(Guid id)
    {
        return await context.OrganizationalUnits
            .Include(ou => ou.UserOrganizationalUnits)
            .Include(ou => ou.Children)
            .Include(ou => ou.Computers)
            .FirstOrDefaultAsync(ou => ou.Id == id);
    }

    public async Task<IEnumerable<OrganizationalUnit>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.OrganizationalUnits
            .Include(ou => ou.UserOrganizationalUnits)
            .Include(ou => ou.Children)
            .Include(ou => ou.Computers)
            .Where(ou => ids.Contains(ou.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<OrganizationalUnit>> GetAllAsync()
    {
        return await context.OrganizationalUnits
            .Include(ou => ou.UserOrganizationalUnits)
            .Include(ou => ou.Children)
            .Include(ou => ou.Computers)
            .ToListAsync();
    }

    public async Task<IEnumerable<OrganizationalUnit>> FindAsync(Expression<Func<OrganizationalUnit, bool>> predicate)
    {
        return await context.OrganizationalUnits
            .Include(ou => ou.UserOrganizationalUnits)
            .Include(ou => ou.Children)
            .Include(ou => ou.Computers)
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(OrganizationalUnit entity)
    {
        await context.OrganizationalUnits.AddAsync(entity);
    }

    public async Task UpdateAsync(OrganizationalUnit entity)
    {
        context.OrganizationalUnits.Update(entity);
    }

    public async Task DeleteAsync(OrganizationalUnit entity)
    {
        context.OrganizationalUnits.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Computer>> FindComputersAsync(Expression<Func<Computer, bool>> predicate)
    {
        return await context.OrganizationalUnits
            .SelectMany(ou => ou.Computers)
            .Where(predicate)
            .ToListAsync();
    }

    public async Task RemoveComputerAsync(OrganizationalUnit organizationalUnit, Computer computer)
    {
        ArgumentNullException.ThrowIfNull(organizationalUnit);
        ArgumentNullException.ThrowIfNull(computer);

        organizationalUnit.RemoveComputer(computer.Id);
        context.Computers.Remove(computer);

        await SaveChangesAsync();
    }
}
