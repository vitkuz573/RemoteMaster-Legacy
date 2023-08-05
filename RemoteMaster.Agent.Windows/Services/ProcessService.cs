// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Shared.Native.Windows;

namespace RemoteMaster.Agent.Services;

public class ProcessService : IProcessService
{
    public void Start(string path)
    {
        ProcessHelper.OpenInteractiveProcess(path, -1, true, "default", true, out _);
    }
}
