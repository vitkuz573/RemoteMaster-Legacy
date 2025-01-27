// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.AuditLogAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class AuditLogRepository(AuditLogDbContext context) : IAuditLogRepository
{
    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        return await context.AuditLogs
            .FirstOrDefaultAsync(ac => ac.Id == id);
    }

    public async Task<IEnumerable<AuditLog>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.AuditLogs
            .Where(ac => ids.Contains(ac.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await context.AuditLogs
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> FindAsync(Expression<Func<AuditLog, bool>> predicate)
    {
        return await context.AuditLogs
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(AuditLog entity)
    {
        await context.AuditLogs.AddAsync(entity);
    }

    public void Update(AuditLog entity)
    {
        context.AuditLogs.Update(entity);
    }

    public void Delete(AuditLog entity)
    {
        context.AuditLogs.Remove(entity);
    }
}
