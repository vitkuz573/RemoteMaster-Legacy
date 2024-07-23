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
        hostApplicationLifetime.ApplicationStarted.Register(Callback);

        return Task.CompletedTask;

        async void Callback()
        {
            var folderPath = configuration["folder-path"];

            if (folderPath == null)
            {
                throw new ArgumentNullException(folderPath, "Configuration 'folder-path' cannot be null or empty");
            }

            var username = configuration["username"];
            var password = configuration["password"];

            var force = bool.TryParse(configuration["force"], out var forceUpdate) && forceUpdate;
            var allowDowngrade = bool.TryParse(configuration["allow-downgrade"], out var allow) && allow;

            await Task.Delay(2000, cancellationToken);

            await hostUpdater.UpdateAsync(folderPath, username, password, force, allowDowngrade);

            Environment.Exit(0);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}