// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class UninstallMode : LaunchModeBase
{
    public override string Name => "Uninstall";

    public override string Description => "Removes the program and its components.";

    protected override void InitializeParameters()
    {
    }
}
