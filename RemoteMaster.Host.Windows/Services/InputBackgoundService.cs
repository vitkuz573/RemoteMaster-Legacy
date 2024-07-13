// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

internal class InputBackgroundService(IInputService inputService) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        inputService.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        inputService.Dispose();

        return Task.CompletedTask;
    }
}
