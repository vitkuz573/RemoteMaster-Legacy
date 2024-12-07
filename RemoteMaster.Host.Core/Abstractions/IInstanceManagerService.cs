// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.EventArguments;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IInstanceManagerService
{
    event EventHandler<InstanceStartedEventArgs>? InstanceStarted;

    int StartNewInstance(string? destinationPath, string commandName, string[] arguments, ProcessStartInfo startInfo, INativeProcessOptions? options = null);
}
