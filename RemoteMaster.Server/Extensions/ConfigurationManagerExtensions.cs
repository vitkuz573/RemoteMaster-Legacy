// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Providers;

namespace RemoteMaster.Server.Extensions;

public static class ConfigurationManagerExtensions
{
    public static ConfigurationManager AddTelegramBotConfiguration(this ConfigurationManager manager, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(manager);

        IConfigurationBuilder configBuilder = manager;
        configBuilder.Add(new TelegramBotConfigurationSource(serviceProvider));

        return manager;
    }
}
