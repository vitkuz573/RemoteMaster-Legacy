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
    private readonly ITelegramBotClient? _botClient;
    private readonly List<string>? _chatIds;
    private readonly bool _isConfigured;

    public TelegramEventNotificationService(IOptions<TelegramBotOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var telegramOptions = options.Value;

        if (string.IsNullOrWhiteSpace(telegramOptions.BotToken) || telegramOptions.ChatIds.Count == 0)
        {
            _isConfigured = false;

            Log.Warning("Telegram bot configuration is missing or incomplete. Notifications will be ignored.");
        }
        else
        {
            _botClient = new TelegramBotClient(telegramOptions.BotToken);
            _chatIds = telegramOptions.ChatIds;
            _isConfigured = true;
        }
    }

    public async Task<Result> SendNotificationAsync(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_isConfigured)
        {
            return Result.Fail("Telegram bot is not configured.");
        }

        try
        {
            if (_botClient == null || _chatIds == null)
            {
                return Result.Ok();
            }

            var escapedMessage = EscapeMarkdownV2(message);

            foreach (var chatId in _chatIds)
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
