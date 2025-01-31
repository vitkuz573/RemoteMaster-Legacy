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
            if (fileSystem.File.Exists("/etc/os-release"))
            {
                foreach (var line in fileSystem.File.ReadAllLines("/etc/os-release"))
                {
                    if (line.StartsWith("PRETTY_NAME="))
                    {
                        return line.Split('=')[1].Trim('"');
                    }
                }
            }
            else if (fileSystem.File.Exists("/etc/lsb-release"))
            {
                foreach (var line in fileSystem.File.ReadAllLines("/etc/lsb-release"))
                {
                    if (line.StartsWith("DISTRIB_DESCRIPTION="))
                    {
                        return line.Split('=')[1].Trim('"');
                    }
                }
            }

            return "Unknown Linux Distribution";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve Linux OS name", ex);
        }
    }
}
