// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using static Windows.Wdk.PInvoke;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Extensions;

public static class ProcessExtensions
{
    public static string GetCommandLine(this Process process)
    {
        ArgumentNullException.ThrowIfNull(process);

        using var processHandle = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION, false, (uint)process.Id);

        if (processHandle.IsInvalid)
        {
            throw new InvalidOperationException($"Unable to open process with ID {process.Id}. Error code: {Marshal.GetLastWin32Error()}");
        }

        var buffer = new byte[4096];
        uint returnLength = 0;

        NTSTATUS status;

        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
                status = NtQueryInformationProcess((HANDLE)processHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessCommandLineInformation, bufferPtr, (uint)buffer.Length, ref returnLength);
            }
        }

        if (status.SeverityCode != NTSTATUS.Severity.Success)
        {
            throw new InvalidOperationException($"NtQueryInformationProcess failed with status code: {status}");
        }

        return Encoding.Unicode.GetString(buffer, 0, (int)returnLength);
    }
}
