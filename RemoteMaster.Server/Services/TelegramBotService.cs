// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.TelegramBotAggregate;

namespace RemoteMaster.Server.Services;

public class TelegramBotService(ITelegramBotUnitOfWork telegramBotUnitOfWork) : ITelegramBotService
{
    public async Task<TelegramBot?> GetBotSettingsAsync()
    {
        var telegramBot = (await telegramBotUnitOfWork.TelegramBots.GetAllAsync()).FirstOrDefault() ?? new TelegramBot();

        if (telegramBot.Id == 0)
        {
            await telegramBotUnitOfWork.TelegramBots.AddAsync(telegramBot);
        }

        return telegramBot;
    }

    public async Task UpdateBotSettingsAsync(TelegramBot telegramBot)
    {
        telegramBotUnitOfWork.TelegramBots.Update(telegramBot);
        await telegramBotUnitOfWork.CommitAsync();
    }

    public async Task AddNewChatIdAsync(int botId, int newChatId)
    {
        var bot = await telegramBotUnitOfWork.TelegramBots.GetByIdAsync(botId) ?? new TelegramBot();
        bot.AddChatId(newChatId);

        await UpdateBotSettingsAsync(bot);
    }

    public async Task RemoveChatIdAsync(int botId, int chatId)
    {
        var bot = await telegramBotUnitOfWork.TelegramBots.GetByIdAsync(botId);

        if (bot != null && bot.ChatIds.Any(c => c.ChatId == chatId))
        {
            bot.RemoveChatId(chatId);

            await UpdateBotSettingsAsync(bot);
        }
    }
}