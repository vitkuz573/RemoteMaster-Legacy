// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ProcessService(IProcessWrapperFactory processWrapperFactory) : IProcessService
{
    public IProcess? GetProcessById(int processId)
    {
        var process = Process.GetProcessById(processId);

        return processWrapperFactory.Create(process);
    }

    public IProcess[] GetProcessesByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        return processes
            .Where(p => !p.HasExited)
            .Select(processWrapperFactory.Create)
            .ToArray();
    }
}
