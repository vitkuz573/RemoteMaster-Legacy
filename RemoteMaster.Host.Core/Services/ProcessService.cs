// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ProcessService(IProcessWrapperFactory processWrapperFactory, ICommandLineProvider commandLineProvider) : IProcessService
{
    public IProcess Start(ProcessStartInfo startInfo)
    {
        var process = processWrapperFactory.Create();

        process.Start(startInfo);

        return process;
    }

    public void WaitForExit(IProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        process.WaitForExit();
    }

    public async Task<string> ReadStandardOutputAsync(IProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        return await process.StandardOutput.ReadToEndAsync();
    }

    public IProcess[] GetProcessesByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        return processes.Where(p => !p.HasExited)
            .Select(p => new ProcessWrapper(p, commandLineProvider))
            .ToArray();
    }

    public bool HasProcessArgument(IProcess process, string argument)
    {
        ArgumentNullException.ThrowIfNull(process);

        var commandLine = process.GetCommandLine();

        return commandLine.Contains(argument);
    }
}
