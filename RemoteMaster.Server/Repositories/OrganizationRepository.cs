// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class OrganizationRepository(ApplicationDbContext context) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Organization>> GetAllAsync()
    {
        return await context.Organizations
            .Include(o => o.OrganizationalUnits)
            .ToListAsync();
    }

    public async Task<IEnumerable<Organization>> FindAsync(Expression<Func<Organization, bool>> predicate)
    {
        return await context.Organizations
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(Organization entity)
    {
        await context.Organizations.AddAsync(entity);
    }

    public async Task UpdateAsync(Organization entity)
    {
        context.Organizations.Update(entity);
    }

    public async Task DeleteAsync(Organization entity)
    {
        context.Organizations.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
