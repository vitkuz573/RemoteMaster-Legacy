// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CertificateRenewalTaskAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class CertificateRenewalTaskRepository(CertificateTaskDbContext context) : ICertificateRenewalTaskRepository
{
    public async Task<CertificateRenewalTask?> GetByIdAsync(Guid id)
    {
        return await context.CertificateRenewalTasks
            .FirstOrDefaultAsync(crt => crt.Id == id);
    }

    public async Task<IEnumerable<CertificateRenewalTask>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.CertificateRenewalTasks
            .Where(crt => ids.Contains(crt.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<CertificateRenewalTask>> GetAllAsync()
    {
        return await context.CertificateRenewalTasks
            .ToListAsync();
    }

    public async Task<IEnumerable<CertificateRenewalTask>> FindAsync(Expression<Func<CertificateRenewalTask, bool>> predicate)
    {
        return await context.CertificateRenewalTasks
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(CertificateRenewalTask entity)
    {
        await context.CertificateRenewalTasks.AddAsync(entity);
    }

    public void Update(CertificateRenewalTask entity)
    {
        context.CertificateRenewalTasks.Update(entity);
    }

    public void Delete(CertificateRenewalTask entity)
    {
        context.CertificateRenewalTasks.Remove(entity);
    }
}
