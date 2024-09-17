// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Aggregates.TelegramBotAggregate;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Providers;

public sealed class TelegramBotConfigurationProvider(IServiceProvider serviceProvider) : ConfigurationProvider
{
    public override void Load()
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TelegramBotDbContext>();

        dbContext.Database.EnsureCreated();

        var bot = dbContext.TelegramBots
            .Include(bot => bot.ChatIds)
            .FirstOrDefault();

        if (bot != null)
        {
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
            Data = CreateAndSaveDefaultValues(dbContext);
        }

        OnReload();
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
