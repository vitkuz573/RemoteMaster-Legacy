// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Services;

public class HiddenWindowService : IHostedService
{
    private readonly HiddenWindow _hiddenWindow;

    public HiddenWindowService(HiddenWindow hiddenWindow)
    {
        _hiddenWindow = hiddenWindow;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hiddenWindow.Initialize();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hiddenWindow.StopMessageLoop();

        return Task.CompletedTask;
    }
}
