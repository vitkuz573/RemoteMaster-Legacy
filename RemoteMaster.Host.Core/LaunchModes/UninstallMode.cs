// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.LaunchModes;

public class UninstallMode : LaunchModeBase
{
    public override string Name => "Uninstall";

    public override string Description => "Removes the program and its components.";

    protected override void InitializeParameters()
    {
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<UninstallMode>>();
        var serverAvailabilityService = serviceProvider.GetRequiredService<IServerAvailabilityService>();
        var hostConfigurationService = serviceProvider.GetRequiredService<IHostConfigurationService>();
        var hostUninstaller = serviceProvider.GetRequiredService<IHostUninstaller>();

        var currentConfig = await hostConfigurationService.LoadConfigurationAsync();

        var server = currentConfig.Server;

        if (!await serverAvailabilityService.IsServerAvailableAsync(server, cancellationToken))
        {
            logger.LogError("The server {Server} is unavailable. Uninstallation will not proceed.", server);
            Environment.Exit(1);
        }

        await hostUninstaller.UninstallAsync();

        Environment.Exit(0);
    }
}
