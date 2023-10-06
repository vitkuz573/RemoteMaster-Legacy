// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.System.Threading;

namespace RemoteMaster.Shared.Models;

public class NativeProcess
{
    public uint ProcessId { get; }

    public uint ThreadId { get; }

    public SafeFileHandle ProcessHandle { get; }

    public SafeFileHandle ThreadHandle { get; }

    public SafeFileHandle StdOutputReadHandle { get; }

    public NativeProcess(PROCESS_INFORMATION procInfo, SafeFileHandle stdOutputReadHandle)
    {
        ProcessId = procInfo.dwProcessId;
        ThreadId = procInfo.dwThreadId;
        ProcessHandle = new SafeFileHandle(procInfo.hProcess, true);
        ThreadHandle = new SafeFileHandle(procInfo.hThread, true);
        StdOutputReadHandle = stdOutputReadHandle;
    }

    public event Action<string> OutputReceived;

    public async Task StartListeningToOutputAsync()
    {
        if (StdOutputReadHandle == null || StdOutputReadHandle.IsInvalid)
        {
            throw new InvalidOperationException("Invalid standard output handle.");
        }

        using var fs = new FileStream(StdOutputReadHandle, FileAccess.Read, 4096, true);
        using var reader = new StreamReader(fs, Encoding.UTF8);

        string line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            OutputReceived?.Invoke(line);
        }
    }
}
