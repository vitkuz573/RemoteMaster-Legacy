// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Aggregates.TelegramBotAggregate;

public class TelegramBot : IAggregateRoot
{
    public TelegramBot()
    {
        BotToken = "test";
    }

    private readonly List<TelegramBotChatId> _chatIds = [];

    public int Id { get; private set; }

    public bool IsEnabled { get; private set; }

    public string BotToken { get; private set; }

    public IReadOnlyCollection<TelegramBotChatId> ChatIds => _chatIds.AsReadOnly();

    public void AddChatId(int chatId)
    {
        var telegramBotChatId = new TelegramBotChatId(chatId);

        _chatIds.Add(telegramBotChatId);
    }

    public void RemoveChatId(int chatId)
    {
        var telegramBotChatId = _chatIds.FirstOrDefault(c => c.ChatId == chatId);

        if (telegramBotChatId != null)
        {
            _chatIds.Remove(telegramBotChatId);
        }
    }

    public void EnableBot()
    {
        IsEnabled = true;
    }

    public void DisableBot()
    {
        IsEnabled = false;
    }

    private void UpdateBotToken(string newToken)
    {
        if (!string.IsNullOrEmpty(newToken))
        {
            BotToken = newToken;
        }
    }

    public void UpdateSettings(bool isEnabled, string botToken)
    {
        IsEnabled = isEnabled;
        UpdateBotToken(botToken);
    }
}
