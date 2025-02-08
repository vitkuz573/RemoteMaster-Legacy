// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IProcessService
{
    IProcess Start(ProcessStartInfo startInfo);

    void WaitForExit(IProcess process);

    Task<string> ReadStandardOutputAsync(IProcess process);

    IProcess[] GetProcessesByName(string processName);

    bool HasProcessArgument(IProcess process, string argument);
}
