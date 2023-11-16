// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Management;

namespace RemoteMaster.Host.Windows.Extensions;

public static class ProcessExtensions
{
    public static string GetCommandLine(this Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}";

        using var searcher = new ManagementObjectSearcher(query);
        using var objects = searcher.Get();

        return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString() ?? string.Empty;
    }
}
