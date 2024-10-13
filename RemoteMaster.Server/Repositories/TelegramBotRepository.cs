// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.TelegramBotAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Repositories;

public class TelegramBotRepository(TelegramBotDbContext context) : ITelegramBotRepository
{
    public async Task<TelegramBot?> GetByIdAsync(int id)
    {
        return await context.TelegramBots
            .Include(tb => tb.ChatIds)
            .FirstOrDefaultAsync(tb => tb.Id == id);
    }

    public async Task<IEnumerable<TelegramBot>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await context.TelegramBots
            .Include(tb => tb.ChatIds)
            .Where(tb => ids.Contains(tb.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<TelegramBot>> GetAllAsync()
    {
        return await context.TelegramBots
            .Include(tb => tb.ChatIds)
            .ToListAsync();
    }

    public async Task<IEnumerable<TelegramBot>> FindAsync(Expression<Func<TelegramBot, bool>> predicate)
    {
        return await context.TelegramBots
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(TelegramBot entity)
    {
        await context.TelegramBots.AddAsync(entity);
    }

    public void Update(TelegramBot entity)
    {
        context.TelegramBots.Update(entity);
    }

    public void Delete(TelegramBot entity)
    {
        context.TelegramBots.Remove(entity);
    }
}
