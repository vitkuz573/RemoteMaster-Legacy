// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ProcessService(IProcessWrapperFactory processWrapperFactory) : IProcessService
{
    public IProcessWrapper Start(ProcessStartInfo startInfo)
    {
        return processWrapperFactory.Create(startInfo);
    }

    public void WaitForExit(IProcessWrapper process)
    {
        ArgumentNullException.ThrowIfNull(process);

        process.WaitForExit();
    }

    public string ReadStandardOutput(IProcessWrapper process)
    {
        ArgumentNullException.ThrowIfNull(process);

        return process.ReadStandardOutput();
    }

    public IProcessWrapper[] FindProcessesByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        return processes.Select(p => new ProcessWrapper(p)).ToArray();
    }

    public bool HasProcessArgument(IProcessWrapper process, string argument)
    {
        ArgumentNullException.ThrowIfNull(process);

        var commandLine = process.GetCommandLine();

        return commandLine.Contains(argument);
    }
}
