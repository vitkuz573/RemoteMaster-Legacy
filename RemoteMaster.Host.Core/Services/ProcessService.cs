// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ProcessService(IProcessWrapperFactory processWrapperFactory, ICommandLineProvider commandLineProvider) : IProcessService
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

    public async Task<string> ReadStandardOutputAsync(IProcessWrapper process)
    {
        ArgumentNullException.ThrowIfNull(process);

        return await process.StandardOutput.ReadToEndAsync();
    }

    public IProcessWrapper[] FindProcessesByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        return processes.Where(p => !p.HasExited)
            .Select(p => new ProcessWrapper(p, commandLineProvider))
            .ToArray();
    }

    public bool HasProcessArgument(IProcessWrapper process, string argument)
    {
        ArgumentNullException.ThrowIfNull(process);

        var commandLine = process.GetCommandLine();

        return commandLine.Contains(argument);
    }
}
