// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Aggregates.TelegramBotAggregate;

public class TelegramBotChatId
{
    private TelegramBotChatId() { }

    internal TelegramBotChatId(int chatId)
    {
        ChatId = chatId;
    }

    public int Id { get; private set; }

    public int ChatId { get; private set; }

    public int TelegramBotId { get; private set; }

    public TelegramBot TelegramBot { get; private set; }
}
