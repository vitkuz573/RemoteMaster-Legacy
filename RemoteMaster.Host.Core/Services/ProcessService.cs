// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ProcessService(ICommandLineProvider commandLineProvider) : IProcessService
{
    public IProcess? GetProcessById(int processId)
    {
        var process = Process.GetProcessById(processId);

        return new ProcessWrapper(process, commandLineProvider);
    }

    public IProcess[] GetProcessesByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        return processes.Where(p => !p.HasExited)
            .Select(p => new ProcessWrapper(p, commandLineProvider))
            .ToArray();
    }
}
