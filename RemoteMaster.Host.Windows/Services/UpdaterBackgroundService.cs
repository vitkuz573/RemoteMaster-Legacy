// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterBackgroundService(UpdateParameters updateParameters, IHostApplicationLifetime appLifetime, IHostUpdater hostUpdater) : IHostedService
{
    private readonly UpdateParameters _updateParameters = updateParameters;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () => await UpdateAsync(), cancellationToken);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task UpdateAsync()
    {
        Thread.Sleep(3000);

        await hostUpdater.UpdateAsync(_updateParameters.FolderPath, updateParameters.Username, updateParameters.Password);

        Environment.Exit(0);
    }
}
