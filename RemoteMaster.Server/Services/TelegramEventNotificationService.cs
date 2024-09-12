// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RemoteMaster.Server.Services;

public class TelegramEventNotificationService : IEventNotificationService
{
    private ITelegramBotClient? _botClient;
    private readonly IOptionsSnapshot<TelegramBotOptions> _optionsSnapshot;
    private TelegramBotOptions _currentOptions;

    public TelegramEventNotificationService(IOptionsSnapshot<TelegramBotOptions> optionsSnapshot)
    {
        ArgumentNullException.ThrowIfNull(optionsSnapshot);

        _optionsSnapshot = optionsSnapshot;
        _currentOptions = _optionsSnapshot.Value;

        if (_currentOptions.IsEnabled)
        {
            _botClient = new TelegramBotClient(_currentOptions.BotToken);
        }
    }

    public async Task UpdateSettingsAsync(TelegramBotOptions options)
    {
        _currentOptions = options;

        if (_currentOptions.IsEnabled)
        {
            _botClient = new TelegramBotClient(_currentOptions.BotToken);
        }
    }

    public async Task<Result> SendNotificationAsync(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_currentOptions.IsEnabled || _botClient == null || !_currentOptions.ChatIds.Any())
        {
            return Result.Fail("Telegram bot is not configured or is disabled.");
        }

        try
        {
            var escapedMessage = EscapeMarkdownV2(message);

            foreach (var chatId in _currentOptions.ChatIds)
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
