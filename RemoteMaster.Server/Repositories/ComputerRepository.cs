// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Repositories;

public class ComputerRepository(ApplicationDbContext context) : IComputerRepository
{
    public async Task<Computer?> GetByIdAsync(Guid id)
    {
        return await context.Computers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Computer>> GetAllAsync()
    {
        return await context.Computers
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Computer>> FindAsync(Expression<Func<Computer, bool>> predicate)
    {
        return await context.Computers
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(Computer entity)
    {
        await context.Computers.AddAsync(entity);
    }

    public async Task UpdateAsync(Computer entity)
    {
        context.Computers.Update(entity);
    }

    public async Task DeleteAsync(Computer entity)
    {
        context.Computers.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
