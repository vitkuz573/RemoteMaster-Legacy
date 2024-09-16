// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Providers;

public sealed class TelegramBotConfigurationSource(TelegramBotDbContext telegramBotDbContext) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new TelegramBotConfigurationProvider(telegramBotDbContext);
    }
}
