// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.TelegramBotAggregate;
using RemoteMaster.Server.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RemoteMaster.Server.Services;

public class TelegramEventNotificationService : IEventNotificationService
{
    private readonly ITelegramBotRepository _repository;
    private ITelegramBotClient? _botClient;
    private readonly TelegramBot? _currentBot;

    public TelegramEventNotificationService(ITelegramBotRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        var bots = _repository.GetAllAsync().Result;
        _currentBot = bots.FirstOrDefault();

        if (_currentBot is { IsEnabled: true })
        {
            _botClient = new TelegramBotClient(_currentBot.BotToken);
        }
    }

    public async Task UpdateSettingsAsync(TelegramBotOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_currentBot == null)
        {
            throw new InvalidOperationException("No TelegramBot found in database.");
        }

        _currentBot.UpdateSettings(options.IsEnabled, options.BotToken);

        await _repository.UpdateAsync(_currentBot);
        await _repository.SaveChangesAsync();

        _botClient = _currentBot.IsEnabled ? new TelegramBotClient(_currentBot.BotToken) : null;
    }

    public async Task<Result> SendNotificationAsync(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_currentBot is not { IsEnabled: true } || _botClient == null || !_currentBot.ChatIds.Any())
        {
            return Result.Fail("Telegram bot is not configured or is disabled.");
        }

        try
        {
            var escapedMessage = EscapeMarkdownV2(message);

            foreach (var chatId in _currentBot.ChatIds.Select(c => c.ChatId))
            {
                await _botClient.SendTextMessageAsync(chatId, escapedMessage, parseMode: ParseMode.MarkdownV2);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending Telegram message");

            return Result.Fail("Error sending Telegram message.").WithError(ex.Message);
        }
    }

    private static string EscapeMarkdownV2(string message)
    {
        return message
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }
}
