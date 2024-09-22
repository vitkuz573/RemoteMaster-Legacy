// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class WoLInitializationService(IWoLConfiguratorService wolConfiguratorService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        wolConfiguratorService.DisableFastStartup();
        wolConfiguratorService.DisablePnPEnergySaving();

        await wolConfiguratorService.EnableWakeOnLanForAllAdaptersAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
