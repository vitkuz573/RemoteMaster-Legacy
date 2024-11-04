// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class ChatMode : LaunchModeBase
{
    public override string Name => "Chat";

    public override string Description => "Runs the program in chat mode, enabling communication features.";

    protected override void InitializeParameters()
    {
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider)
    {
        await Task.CompletedTask;
    }
}
