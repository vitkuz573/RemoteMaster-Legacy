// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Aggregates.TelegramBotAggregate;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Providers;

public sealed class TelegramBotConfigurationProvider(TelegramBotDbContext telegramBotDbContext) : ConfigurationProvider
{
    public override void Load()
    {
        telegramBotDbContext.Database.EnsureCreated();

        if (telegramBotDbContext.TelegramBots.Any(bot => bot.IsEnabled))
        {
            var bot = telegramBotDbContext.TelegramBots
                .Where(bot => bot.IsEnabled)
                .Include(bot => bot.ChatIds)
                .FirstOrDefault();

            if (bot == null)
            {
                return;
            }

            Data["TelegramBot:BotToken"] = bot.BotToken;
            Data["TelegramBot:IsEnabled"] = bot.IsEnabled.ToString();

            var i = 0;

            foreach (var chatId in bot.ChatIds)
            {
                Data[$"TelegramBot:ChatIds:{i}"] = chatId.ChatId.ToString();
                i++;
            }
        }
        else
        {
            Data = CreateAndSaveDefaultValues(telegramBotDbContext);
        }
    }

    private static Dictionary<string, string?> CreateAndSaveDefaultValues(TelegramBotDbContext context)
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["TelegramBot:BotToken"] = "default-token",
            ["TelegramBot:ChatIds:0"] = "1234",
            ["TelegramBot:IsEnabled"] = "true"
        };

        var defaultBot = new TelegramBot();
        defaultBot.UpdateSettings(isEnabled: true, botToken: "default-token");
        defaultBot.AddChatId(1234);

        context.TelegramBots.Add(defaultBot);
        context.SaveChanges();

        return settings;
    }
}
