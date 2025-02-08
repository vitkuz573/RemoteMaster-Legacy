// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IProcessService
{
    IProcess[] GetProcesses();

    IProcess GetCurrentProcess();

    IProcess? GetProcessById(int processId);

    IProcess[] GetProcessesByName(string processName);
}
