// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IProcessService
{
    Process Start(ProcessStartInfo startInfo);

    void WaitForExit(Process process);

    string ReadStandardOutput(Process process);
}
