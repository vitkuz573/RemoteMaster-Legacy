// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using System.Net;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class ApplicationUserRepository(ApplicationDbContext context) : IApplicationUserRepository
{
    public async Task<ApplicationUser?> GetByIdAsync(string id)
    {
        return await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<ApplicationUser>> GetByIdsAsync(IEnumerable<string> ids)
    {
        return await context.Users
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();
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

    public void Update(ApplicationUser entity)
    {
        context.Users.Update(entity);
    }

    public void Delete(ApplicationUser entity)
    {
        context.Users.Remove(entity);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }

    public async Task AddSignInEntryAsync(string userId, bool isSuccessful, IPAddress ipAddress)
    {
        var user = await GetByIdAsync(userId);

        var signInEntry = user?.AddSignInEntry(isSuccessful, ipAddress);

        await context.SignInEntries.AddAsync(signInEntry);
    }

    public async Task<IEnumerable<SignInEntry>> GetAllSignInEntriesAsync()
    {
        return await context.SignInEntries
            .Include(entry => entry.User)
            .OrderByDescending(e => e.SignInTime)
            .ToListAsync();
    }

    public async Task ClearSignInEntriesAsync()
    {
        var allEntries = context.SignInEntries;

        context.SignInEntries.RemoveRange(allEntries);
    }
}
