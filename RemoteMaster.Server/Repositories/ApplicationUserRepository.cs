// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Repositories;

public class ApplicationUserRepository(ApplicationDbContext context) : IApplicationUserRepository
{
    public async Task<ApplicationUser?> GetByIdAsync(string id)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
    {
        return await context.Users
            .ToListAsync();
    }

    public async Task<IEnumerable<ApplicationUser>> FindAsync(Expression<Func<ApplicationUser, bool>> predicate)
    {
        return await context.Users
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(ApplicationUser entity)
    {
        await context.Users.AddAsync(entity);
    }

    public async Task UpdateAsync(ApplicationUser entity)
    {
        context.Users.Update(entity);
    }

    public async Task DeleteAsync(ApplicationUser entity)
    {
        context.Users.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task AddSignInEntryAsync(string userId, bool isSuccessful, string ipAddress)
    {
        var user = await GetByIdAsync(userId);

        var signInEntry = user?.AddSignInEntry(isSuccessful, ipAddress);

        await context.SignInEntries.AddAsync(signInEntry);
    }
}
