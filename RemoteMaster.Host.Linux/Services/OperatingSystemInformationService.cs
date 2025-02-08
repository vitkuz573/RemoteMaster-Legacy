// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// Service for retrieving information about the Linux operating system.
/// </summary>
public sealed class OperatingSystemInformationService(IFileSystem fileSystem) : IOperatingSystemInformationService
{
    private const string UnknownLinuxDistribution = "Unknown Linux Distribution";

    /// <inheritdoc />
    public string GetName()
    {
        try
        {
            var releaseFiles = new[]
            {
                (FilePath: "/etc/os-release", Key: "PRETTY_NAME"),
                (FilePath: "/etc/lsb-release", Key: "DISTRIB_DESCRIPTION")
            };

            foreach (var (filePath, key) in releaseFiles)
            {
                var osName = GetValueFromFile(filePath, key);

                if (!string.IsNullOrWhiteSpace(osName))
                {
                    return osName;
                }
            }

            return UnknownLinuxDistribution;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve Linux OS name.", ex);
        }
    }

    /// <summary>
    /// Attempts to extract the value associated with the specified key from a file.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="key">The key whose value needs to be found.</param>
    /// <returns>
    /// The value, trimmed of whitespace and quotes, if the key is found; otherwise, <c>null</c>.
    /// </returns>
    private string? GetValueFromFile(string filePath, string key)
    {
        if (!fileSystem.File.Exists(filePath))
        {
            return null;
        }

        foreach (var line in fileSystem.File.ReadLines(filePath))
        {
            if (line.StartsWith($"{key}="))
            {
                var parts = line.Split('=', 2);

                if (parts.Length == 2)
                {
                    return parts[1].Trim().Trim('"');
                }
            }
        }

        return null;
    }
}
