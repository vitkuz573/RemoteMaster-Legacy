// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CrlAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class CrlRepository(CertificateDbContext context) : ICrlRepository
{
    public async Task<Crl?> GetByIdAsync(int id)
    {
        return await context.CertificateRevocationLists
            .Include(c => c.RevokedCertificates)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Crl>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await context.CertificateRevocationLists
            .Include(c => c.RevokedCertificates)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<Crl>> GetAllAsync()
    {
        return await context.CertificateRevocationLists
            .Include(c => c.RevokedCertificates)
            .ToListAsync();
    }

    public async Task<IEnumerable<Crl>> FindAsync(Expression<Func<Crl, bool>> predicate)
    {
        return await context.CertificateRevocationLists
            .Include(c => c.RevokedCertificates)
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(Crl entity)
    {
        await context.CertificateRevocationLists.AddAsync(entity);
    }

    public async Task UpdateAsync(Crl entity)
    {
        context.CertificateRevocationLists.Update(entity);
    }

    public async Task DeleteAsync(Crl entity)
    {
        context.CertificateRevocationLists.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
