// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// Provides functionality to retrieve the command line arguments of a process from the /proc file system.
/// </summary>
public class CommandLineProvider(IFileSystem fileSystem) : ICommandLineProvider
{
    /// <inheritdoc/>
    public async Task<string[]> GetCommandLineAsync(IProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        var cmdlinePath = $"/proc/{process.Id}/cmdline";

        if (!fileSystem.File.Exists(cmdlinePath))
        {
            throw new InvalidOperationException($"The command line file for process with ID {process.Id} does not exist at '{cmdlinePath}'.");
        }

        try
        {
            var bytes = await fileSystem.File.ReadAllBytesAsync(cmdlinePath);
            
            return ParseCommandLine(bytes);
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

    /// <summary>
    /// Parses the null-delimited command line byte array into an array of UTF-8 encoded arguments.
    /// </summary>
    /// <param name="bytes">The byte array representing the process command line.</param>
    /// <returns>An array of command line arguments.</returns>
    private static string[] ParseCommandLine(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length == 0)
        {
            return [];
        }

        var arguments = new List<string>(8);
        var span = (ReadOnlySpan<byte>)bytes.AsSpan();
        var start = 0;

        while (start < span.Length)
        {
            var end = span[start..].IndexOf((byte)0);
            
            if (end == -1)
            {
                end = span.Length - start;
            }

            if (end > 0)
            {
                arguments.Add(Encoding.UTF8.GetString(span.Slice(start, end)));
            }

            start += end + 1;
        }

        return [.. arguments];
    }
}
