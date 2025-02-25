// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Extensions;

public static class ProcessExtensions
{
    public static async Task<bool> HasArgumentAsync(this IProcess process, string argument)
    {
        ArgumentNullException.ThrowIfNull(process);

        var commandLine = await process.GetCommandLineAsync();

        return commandLine.Contains(argument);
    }
}
