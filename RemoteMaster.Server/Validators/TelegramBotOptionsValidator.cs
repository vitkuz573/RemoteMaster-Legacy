// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Options;
using RemoteMaster.Server.Options;

namespace RemoteMaster.Server.Validators;

public class TelegramBotOptionsValidator : IValidateOptions<TelegramBotOptions>
{
    public ValidateOptionsResult Validate(string name, TelegramBotOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsEnabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (string.IsNullOrWhiteSpace(options.BotToken))
        {
            return ValidateOptionsResult.Fail("BotToken is required when Telegram bot is enabled.");
        }

        if (options.ChatIds == null || options.ChatIds.Count == 0)
        {
            return ValidateOptionsResult.Fail("At least one chat ID is required when Telegram bot is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
