// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.LaunchModes;

public class ServiceMode : LaunchModeBase
{
    public override string Name => "Service";

    public override string Description => "Runs the program as a service.";

    protected override void InitializeParameters()
    {
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
    }
}
