// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Extensions;

public static class ProcessExtensions
{
    public static bool HasArgument(this IProcess process, string argument)
    {
        ArgumentNullException.ThrowIfNull(process);

        var commandLine = process.GetCommandLine();

        return commandLine.Contains(argument);
    }
}
