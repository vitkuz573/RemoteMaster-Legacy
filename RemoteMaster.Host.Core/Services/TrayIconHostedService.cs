// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class TrayIconHostedService(ITrayIconManager trayIconManager) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        trayIconManager.Show();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        trayIconManager.Hide();

        return Task.CompletedTask;
    }
}
