// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Win32.SafeHandles;
using Windows.Win32.System.Threading;

namespace RemoteMaster.Shared.Models;

public class NativeProcess
{
    public uint ProcessId { get; }

    public uint ThreadId { get; }

    public SafeFileHandle ProcessHandle { get; }

    public SafeFileHandle ThreadHandle { get; }

    public NativeProcess(PROCESS_INFORMATION procInfo)
    {
        ProcessId = procInfo.dwProcessId;
        ThreadId = procInfo.dwThreadId;
        ProcessHandle = new SafeFileHandle(procInfo.hProcess, true);
        ThreadHandle = new SafeFileHandle(procInfo.hThread, true);
    }
}