// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ProcessService : IProcessService
{
    public Process Start(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo);
    }

    public void WaitForExit(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        process.WaitForExit();
    }

    public string ReadStandardOutput(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        return process.StandardOutput.ReadToEnd();
    }
}
