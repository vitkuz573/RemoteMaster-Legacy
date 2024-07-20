// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
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

        if (string.IsNullOrWhiteSpace(telegramOptions.BotToken) || telegramOptions.ChatIds == null || telegramOptions.ChatIds.Count == 0)
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

    public async Task SendNotificationAsync(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_isConfigured)
        {
            return;
        }

        try
        {
            if (_botClient != null && _chatIds != null)
            {
                var escapedMessage = EscapeMarkdownV2(message);

                foreach (var chatId in _chatIds)
                {
                    await _botClient.SendTextMessageAsync(chatId, escapedMessage, parseMode: ParseMode.MarkdownV2);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending Telegram message");
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
