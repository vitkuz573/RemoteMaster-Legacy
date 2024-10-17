// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public abstract class LaunchModeBase
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    public Dictionary<string, ILaunchParameter> Parameters { get; } = [];

    protected abstract void InitializeParameters();

    protected LaunchModeBase()
    {
        InitializeParameters();
    }
}
