// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IProcessService
{
    IProcessWrapper Start(ProcessStartInfo startInfo);

    void WaitForExit(IProcessWrapper process);

    Task<string> ReadStandardOutputAsync(IProcessWrapper process);

    IProcessWrapper[] FindProcessesByName(string processName);

    bool HasProcessArgument(IProcessWrapper process, string argument);
}
