// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text;
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
            throw new InvalidOperationException($"The command line file for process with ID {process.Id} does not exist at '{cmdlinePath}'.");
        }

        try
        {
            var bytes = fileSystem.File.ReadAllBytes(cmdlinePath);
            
            if (bytes.Length == 0)
            {
                return [];
            }

            var arguments = new List<string>(capacity: 8);
            ReadOnlySpan<byte> span = bytes.AsSpan();
            var start = 0;
            
            for (var i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == 0)
                {
                    if (i > start)
                    {
                        var argument = Encoding.UTF8.GetString(span[start..i]);
                        arguments.Add(argument);
                    }

                    start = i + 1;
                }
            }

            return [.. arguments];
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"Insufficient permissions to read the file '{cmdlinePath}'.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve the command line for process with ID {process.Id}.", ex);
        }
    }
}
