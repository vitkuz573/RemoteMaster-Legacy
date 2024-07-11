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

            if (folderPath == null)
            {
                throw new ArgumentNullException(folderPath, "Configuration 'folder-path' cannot be null or empty");
            }

            var username = configuration["username"];
            var password = configuration["password"];

            var force = configuration["force"] != null;
            var allowDowngrade = configuration["allow-downgrade"] != null;

            await Task.Delay(2000);

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
