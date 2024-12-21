// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace RemoteMaster.Server.Services;

public class TelegramEventNotificationService(IOptions<TelegramBotOptions> telegramBotOptions, ILogger<TelegramEventNotificationService> logger) : IEventNotificationService
{
    public async Task<Result> SendNotificationAsync(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var botOptions = telegramBotOptions.Value;

        if (!botOptions.IsEnabled || botOptions.ChatIds.Count == 0)
        {
            return Result.Fail("Telegram bot is not configured or is disabled.");
        }

        try
        {
            var botClient = new TelegramBotClient(botOptions.BotToken);

            var escapedMessage = EscapeMarkdownV2(message);

            foreach (var chatId in botOptions.ChatIds)
            {
                await botClient.SendMessage(chatId, escapedMessage, parseMode: ParseMode.MarkdownV2);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending Telegram message");

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
