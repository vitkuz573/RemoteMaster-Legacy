// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.HostMoveRequestAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class HostMoveRequestRepository(HostMoveRequestDbContext context) : IHostMoveRequestRepository
{
    public async Task<HostMoveRequest?> GetByIdAsync(Guid id)
    {
        return await context.HostMoveRequests
            .FirstOrDefaultAsync(hmr => hmr.Id == id);
    }

    public async Task<IEnumerable<HostMoveRequest>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.HostMoveRequests
            .Where(hmr => ids.Contains(hmr.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<HostMoveRequest>> GetAllAsync()
    {
        return await context.HostMoveRequests
            .ToListAsync();
    }

    public async Task<IEnumerable<HostMoveRequest>> FindAsync(Expression<Func<HostMoveRequest, bool>> predicate)
    {
        return await context.HostMoveRequests
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(HostMoveRequest entity)
    {
        await context.HostMoveRequests.AddAsync(entity);
    }

    public void Update(HostMoveRequest entity)
    {
        context.HostMoveRequests.Update(entity);
    }

    public void Delete(HostMoveRequest entity)
    {
        context.HostMoveRequests.Remove(entity);
    }
}
