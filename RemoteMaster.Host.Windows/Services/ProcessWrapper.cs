// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Extensions;

namespace RemoteMaster.Host.Windows.Services;

public class ProcessWrapper(Process process) : IProcessWrapper
{
    public int Id => process.Id;

    public void Kill()
    {
        process.Kill();
    }

    public string GetCommandLine()
    {
        return process.GetCommandLine();
    }
}