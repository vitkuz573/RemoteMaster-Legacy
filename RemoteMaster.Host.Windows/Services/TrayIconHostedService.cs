// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class TrayIconHostedService(ITrayIconManager trayIconManager) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        trayIconManager.ShowTrayIcon();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        trayIconManager.HideTrayIcon();

        return Task.CompletedTask;
    }
}
