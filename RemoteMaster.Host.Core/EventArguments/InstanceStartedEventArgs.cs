// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.EventArguments;

public class InstanceStartedEventArgs(int processId, string commandName, string[] arguments) : EventArgs
{
    public int ProcessId { get; } = processId;
    
    public string CommandName { get; } = commandName;

    public string[] Arguments { get; } = arguments;
}
