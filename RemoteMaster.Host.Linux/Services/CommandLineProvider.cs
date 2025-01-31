// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class CommandLineProvider(IFileSystem fileSystem) : ICommandLineProvider
{
    public string[] GetCommandLine(IProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        var cmdlinePath = $"/proc/{process.Id}/cmdline";

        if (!fileSystem.File.Exists(cmdlinePath))
        {
            throw new InvalidOperationException($"The command-line file for process with ID {process.Id} does not exist.");
        }

        try
        {
            var cmdline = fileSystem.File.ReadAllText(cmdlinePath);

            return string.IsNullOrEmpty(cmdline) ? [] : cmdline.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Insufficient permissions to read the file {cmdlinePath}.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read the command line for process with ID {process.Id}.", ex);
        }
    }
}
