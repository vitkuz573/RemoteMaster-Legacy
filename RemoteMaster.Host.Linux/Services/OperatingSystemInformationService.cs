// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class OperatingSystemInformationService(IFileSystem fileSystem) : IOperatingSystemInformationService
{
    public string GetName()
    {
        try
        {
            var osName = GetValueFromFile("/etc/os-release", "PRETTY_NAME")
                      ?? GetValueFromFile("/etc/lsb-release", "DISTRIB_DESCRIPTION");

            return osName ?? "Unknown Linux Distribution";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve Linux OS name", ex);
        }
    }

    private string? GetValueFromFile(string path, string key)
    {
        if (!fileSystem.File.Exists(path))
        {
            return null;
        }

        var lines = fileSystem.File.ReadAllLines(path);

        var line = lines.FirstOrDefault(l => l.StartsWith($"{key}="));
        
        if (line is null)
        {
            return null;
        }

        var parts = line.Split('=', 2);
        
        return parts.Length < 2 ? null : parts[1].Trim().Trim('"');
    }
}
