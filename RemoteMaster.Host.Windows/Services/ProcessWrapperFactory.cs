// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ProcessWrapperFactory : IProcessWrapperFactory
{
    public IProcessWrapper Create(ProcessStartInfo startInfo)
    {
        var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        return new ProcessWrapper(process);
    }
}
