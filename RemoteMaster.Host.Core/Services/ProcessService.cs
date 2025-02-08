// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ProcessService(IProcessWrapperFactory processWrapperFactory) : IProcessService
{
    public IProcess[] GetProcesses()
    {
        var processes = Process.GetProcesses();

        return processes
            .Select(processWrapperFactory.Create)
            .ToArray();
    }

    public IProcess GetCurrentProcess()
    {
        var process = Process.GetCurrentProcess();

        return processWrapperFactory.Create(process);
    }

    public IProcess? GetProcessById(int processId)
    {
        var process = Process.GetProcessById(processId);

        return processWrapperFactory.Create(process);
    }

    public IProcess[] GetProcessesByName(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        return processes
            .Select(processWrapperFactory.Create)
            .ToArray();
    }
}
