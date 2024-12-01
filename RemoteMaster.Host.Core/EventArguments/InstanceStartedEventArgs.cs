// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.EventArguments;

public class InstanceStartedEventArgs(int processId, LaunchModeBase launchMode) : EventArgs
{
    public int ProcessId { get; } = processId;
    
    public LaunchModeBase LaunchMode { get; } = launchMode;
}
