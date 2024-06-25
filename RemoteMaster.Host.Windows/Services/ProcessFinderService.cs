// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ProcessFinderService : IProcessFinderService
{
    public IProcessWrapper[] FindHostProcesses(string executablePath)
    {
        var processName = Path.GetFileNameWithoutExtension(executablePath);
        var processes = Process.GetProcessesByName(processName);

        return processes.Select(p => new ProcessWrapper(p)).ToArray();
    }

    public bool IsUserInstance(IProcessWrapper process, string argument)
    {
        ArgumentNullException.ThrowIfNull(process);

        var commandLine = process.GetCommandLine();

        return commandLine.Contains(argument);
    }
}