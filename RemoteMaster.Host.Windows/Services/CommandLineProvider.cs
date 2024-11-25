// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using static Windows.Wdk.PInvoke;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandLineProvider : ICommandLineProvider
{
    public string GetCommandLine(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        using var processHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)process.Id);

        if (processHandle.IsInvalid)
        {
            throw new InvalidOperationException($"Unable to open process with ID {process.Id}. Error code: {Marshal.GetLastWin32Error()}");
        }

        uint bufferSize = 256;
        uint returnLength = 0;

        while (true)
        {
            var buffer = new byte[bufferSize];
            NTSTATUS status;

            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    status = NtQueryInformationProcess((HANDLE)processHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessCommandLineInformation, bufferPtr, (uint)buffer.Length, ref returnLength);
                }
            }

            if (status.SeverityCode == NTSTATUS.Severity.Success)
            {
                return Encoding.Unicode.GetString(buffer, 0, (int)returnLength);
            }

            if (status == NTSTATUS.STATUS_BUFFER_OVERFLOW || status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                bufferSize = returnLength;

                continue;
            }

            throw new InvalidOperationException($"NtQueryInformationProcess failed with status code: {status}");
        }
    }
}
