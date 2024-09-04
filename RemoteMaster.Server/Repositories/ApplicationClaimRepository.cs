// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationClaimAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class ApplicationClaimRepository(ApplicationDbContext context) : IApplicationClaimRepository
{
    public async Task<ApplicationClaim?> GetByIdAsync(int id)
    {
        return await context.ApplicationClaims
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<ApplicationClaim>> GetAllAsync()
    {
        return await context.ApplicationClaims
            .ToListAsync();
    }

    public async Task<IEnumerable<ApplicationClaim>> FindAsync(Expression<Func<ApplicationClaim, bool>> predicate)
    {
        return await context.ApplicationClaims
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(ApplicationClaim entity)
    {
        await context.ApplicationClaims.AddAsync(entity);
    }

    public async Task UpdateAsync(ApplicationClaim entity)
    {
        context.ApplicationClaims.Update(entity);
    }

    public async Task DeleteAsync(ApplicationClaim entity)
    {
        context.ApplicationClaims.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}