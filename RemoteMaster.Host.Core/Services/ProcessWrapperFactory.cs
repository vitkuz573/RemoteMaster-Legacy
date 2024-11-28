// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ProcessWrapperFactory(ICommandLineProvider commandLineProvider) : IProcessWrapperFactory
{
    public IProcessWrapper Create(ProcessStartInfo startInfo)
    {
#pragma warning disable CA2000
        var process = new Process
        {
            StartInfo = startInfo
        };
#pragma warning restore CA2000

        return new ProcessWrapper(process, commandLineProvider);
    }
}
