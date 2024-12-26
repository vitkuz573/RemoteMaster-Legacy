// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using static Windows.Wdk.PInvoke;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandLineProvider : ICommandLineProvider
{
    public string[] GetCommandLine(IProcess process)
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
                    status = NtQueryInformationProcess((HANDLE)processHandle.DangerousGetHandle(), PROCESSINFOCLASS.ProcessCommandLineInformation, bufferPtr, bufferSize, ref returnLength);
                }
            }

            if (status.SeverityCode == NTSTATUS.Severity.Success)
            {
                if (returnLength < (uint)Marshal.SizeOf<UNICODE_STRING>())
                {
                    throw new InvalidOperationException("Returned data is smaller than UNICODE_STRING structure.");
                }

                UNICODE_STRING unicodeString;

                unsafe
                {
                    fixed (byte* bufferPtr = buffer)
                    {
                        unicodeString = Marshal.PtrToStructure<UNICODE_STRING>((nint)bufferPtr);
                    }

                    if (unicodeString.Buffer.Value == null)
                    {
                        throw new InvalidOperationException("Buffer pointer in UNICODE_STRING is null.");
                    }
                }

                nint commandLinePtr;

                unsafe
                {
                    commandLinePtr = (nint)unicodeString.Buffer.Value;
                }

                var commandLine = Marshal.PtrToStringUni(commandLinePtr, unicodeString.Length / 2);
                var parsedArgs = ParseCommandLine(commandLine);

                return parsedArgs;
            }

            if (status == NTSTATUS.STATUS_BUFFER_OVERFLOW || status == NTSTATUS.STATUS_INFO_LENGTH_MISMATCH)
            {
                bufferSize = returnLength;
                continue;
            }

            throw new InvalidOperationException($"NtQueryInformationProcess failed with status code: {status}");
        }
    }

    private static unsafe string[] ParseCommandLine(string commandLine)
    {
        var argv = CommandLineToArgv(commandLine, out var argc);

        if (argv == null)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to parse command line.");
        }

        try
        {
            var args = new string[argc];

            for (var i = 0; i < argc; i++)
            {
                var arg = argv[i];

                args[i] = Marshal.PtrToStringUni((nint)arg.Value) ?? string.Empty;
            }

            return args;
        }
        finally
        {
            using var handle = LocalFree_SafeHandle(new HLOCAL((nint)argv));
        }
    }
}
