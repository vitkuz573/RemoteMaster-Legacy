// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.TelegramBotAggregate;

namespace RemoteMaster.Server.Abstractions;

public interface ITelegramBotService
{
    Task UpdateBotSettingsAsync(TelegramBot telegramBot);

    Task<TelegramBot?> GetBotSettingsAsync();

    Task AddNewChatIdAsync(int botId, int newChatId);

    Task RemoveChatIdAsync(int botId, int chatId);
}
