// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterBackground(IConfiguration configuration, IHostApplicationLifetime hostApplicationLifetime, IHostUpdater hostUpdater) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        hostApplicationLifetime.ApplicationStarted.Register(async () =>
        {
            var folderPath = configuration["folder-path"];
            var username = configuration["username"];
            var password = configuration["password"];

            var force = configuration["force"] != null;
            var allowDowngrade = configuration["allow-downgrade"] != null;

            Thread.Sleep(3000);

            await hostUpdater.UpdateAsync(folderPath, username, password, force, allowDowngrade);

            Environment.Exit(0);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
