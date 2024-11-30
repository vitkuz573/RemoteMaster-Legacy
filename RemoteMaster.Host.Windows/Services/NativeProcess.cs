// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Console;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class NativeProcess : IProcess
{
    private static readonly Lock CreateProcessLock = new();

    private readonly NativeProcessOptions _options;
    private readonly ISessionService _sessionService;

    private SafeProcessHandle? _processHandle;
    private string? _commandLine;
    private bool _haveProcessId;

    public int Id { get; private set; }

    public StreamWriter? StandardInput { get; private set; }

    public StreamReader? StandardOutput { get; private set; }

    public StreamReader? StandardError { get; private set; }

    public bool HasExited
    {
        get
        {
            if (_processHandle == null || _processHandle.IsInvalid || _processHandle.IsClosed)
            {
                throw new InvalidOperationException("No process is associated with this NativeProcess object.");
            }

            if (GetExitCodeProcess(_processHandle, out var exitCode))
            {
                return exitCode != NTSTATUS.STILL_ACTIVE;
            }

            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public NativeProcess(INativeProcessOptions options, ISessionService sessionService)
    {
        if (options is not NativeProcessOptions nativeOptions)
        {
            throw new ArgumentException("Invalid options type. Expected NativeProcessOptions.", nameof(options));
        }

        _options = nativeOptions;
        _sessionService = sessionService;
    }

    public void Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        _commandLine = $"{startInfo.FileName} {startInfo.Arguments}";

        var sessionId = _options is { TargetSessionId: not null, ForceConsoleSession: false }
            ? _sessionService.FindTargetSessionId(_options.TargetSessionId.Value)
            : _sessionService.GetActiveConsoleSessionId();


        SafeFileHandle? hUserTokenDup = null;

        try
        {
            if (!_options.UseCurrentUserToken || !TryGetUserToken(sessionId, out hUserTokenDup))
            {
                var winlogonPid = _sessionService.GetProcessId(sessionId, "winlogon");
                using var hProcess = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION | PROCESS_ACCESS_RIGHTS.PROCESS_DUP_HANDLE, false, winlogonPid);

                if (!hProcess.IsInvalid && !hProcess.IsClosed)
                {
                    if (OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hPToken))
                    {
                        using (hPToken)
                        {
                            DuplicateTokenEx(hPToken, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE | TOKEN_ACCESS_MASK.TOKEN_QUERY | TOKEN_ACCESS_MASK.TOKEN_ASSIGN_PRIMARY | TOKEN_ACCESS_MASK.TOKEN_IMPERSONATE, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup);
                        }
                    }
                }
            }

            if (hUserTokenDup != null)
            {
                StartWithCreateProcess(startInfo, hUserTokenDup);
            }
            else
            {
                throw new InvalidOperationException("Failed to get or duplicate user token.");
            }
        }
        finally
        {
            hUserTokenDup?.Dispose();
        }
    }

    public void Kill()
    {
        if (_processHandle == null || _processHandle.IsInvalid || _processHandle.IsClosed)
        {
            throw new InvalidOperationException("No process is associated with this NativeProcess object.");
        }

        if (!TerminateProcess(_processHandle, 1))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    private bool StartWithCreateProcess(ProcessStartInfo startInfo, SafeHandle hUserTokenDup)
    {
        STARTUPINFOW startupInfo = default;
        PROCESS_INFORMATION processInfo = default;
        SECURITY_ATTRIBUTES securityAttributes = default;
#pragma warning disable CA2000
        var procSH = new SafeProcessHandle();
#pragma warning restore CA2000

        SafeFileHandle? parentInputPipeHandle = null;
        SafeFileHandle? childInputPipeHandle = null;
        SafeFileHandle? parentOutputPipeHandle = null;
        SafeFileHandle? childOutputPipeHandle = null;
        SafeFileHandle? parentErrorPipeHandle = null;
        SafeFileHandle? childErrorPipeHandle = null;

        using (CreateProcessLock.EnterScope())
        {
            try
            {
                startupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOW>();

                if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                {
                    if (startInfo.RedirectStandardInput)
                    {
#pragma warning disable CA2000
                        CreatePipe(out parentInputPipeHandle, out childInputPipeHandle, true);
#pragma warning restore CA2000
                    }
                    else
                    {
                        childInputPipeHandle = GetStdHandle_SafeHandle(STD_HANDLE.STD_INPUT_HANDLE);
                    }

                    if (startInfo.RedirectStandardOutput)
                    {
#pragma warning disable CA2000
                        CreatePipe(out parentOutputPipeHandle, out childOutputPipeHandle, false);
#pragma warning restore CA2000
                    }
                    else
                    {
                        childOutputPipeHandle = GetStdHandle_SafeHandle(STD_HANDLE.STD_OUTPUT_HANDLE);
                    }

                    if (startInfo.RedirectStandardError)
                    {
#pragma warning disable CA2000
                        CreatePipe(out parentErrorPipeHandle, out childErrorPipeHandle, false);
#pragma warning restore CA2000
                    }
                    else
                    {
                        childErrorPipeHandle = GetStdHandle_SafeHandle(STD_HANDLE.STD_ERROR_HANDLE);
                    }

                    startupInfo.hStdInput = (HANDLE)childInputPipeHandle.DangerousGetHandle();
                    startupInfo.hStdOutput = (HANDLE)childOutputPipeHandle.DangerousGetHandle();
                    startupInfo.hStdError = (HANDLE)childErrorPipeHandle.DangerousGetHandle();

                    startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;
                }

                unsafe
                {
                    fixed (char* pDesktopName = $@"winsta0\{_options.DesktopName}")
                    {
                        startupInfo.lpDesktop = pDesktopName;
                    }
                }

                PROCESS_CREATION_FLAGS dwCreationFlags = 0;

                dwCreationFlags |= startInfo.CreateNoWindow
                    ? PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW
                    : PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;

                dwCreationFlags |= PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;
                var environmentBlock = GetEnvironmentVariablesBlock(startInfo.EnvironmentVariables!);

                var fullCommand = $"{startInfo.FileName} {startInfo.Arguments}";
                fullCommand += char.MinValue;

                var commandSpan = new Span<char>(fullCommand.ToCharArray());

                bool retVal;
                var errorCode = 0;

                unsafe
                {
                    fixed (char* pEnvironmentBlock = environmentBlock)
                    {
                        retVal = CreateProcessAsUser(hUserTokenDup, null, ref commandSpan, securityAttributes, securityAttributes, true, dwCreationFlags, pEnvironmentBlock, null, startupInfo, out processInfo);
                    }
                }

                if (!retVal)
                {
                    errorCode = Marshal.GetLastWin32Error();
                }

                if (processInfo.hProcess != nint.Zero && processInfo.hProcess != new nint(-1))
                {
                    Marshal.InitHandle(procSH, processInfo.hProcess);
                }

                if (processInfo.hThread != nint.Zero && processInfo.hThread != new nint(-1))
                {
                    CloseHandle(processInfo.hThread);
                }
            }
            catch
            {
                parentInputPipeHandle?.Dispose();
                parentOutputPipeHandle?.Dispose();
                parentErrorPipeHandle?.Dispose();
                procSH.Dispose();
                throw;
            }
            finally
            {
                childInputPipeHandle?.Dispose();
                childOutputPipeHandle?.Dispose();
                childErrorPipeHandle?.Dispose();
            }
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        if (startInfo.RedirectStandardInput)
        {
            var enc = startInfo.StandardInputEncoding ?? Encoding.GetEncoding((int)GetConsoleCP());

            StandardInput = new StreamWriter(new FileStream(parentInputPipeHandle!, FileAccess.Write, 4096, false), enc, 4096)
            {
                AutoFlush = true
            };
        }

        if (startInfo.RedirectStandardOutput)
        {
            var enc = startInfo.StandardOutputEncoding ?? Encoding.GetEncoding((int)GetConsoleOutputCP());

            StandardOutput = new StreamReader(new FileStream(parentOutputPipeHandle!, FileAccess.Read, 4096, false), enc, true, 4096);
        }

        if (startInfo.RedirectStandardError)
        {
            var enc = startInfo.StandardErrorEncoding ?? Encoding.GetEncoding((int)GetConsoleOutputCP());

            StandardError = new StreamReader(new FileStream(parentErrorPipeHandle!, FileAccess.Read, 4096, false), enc, true, 4096);
        }

        if (procSH.IsInvalid)
        {
            procSH.Dispose();

            return false;
        }

        _processHandle = new SafeProcessHandle(processInfo.hProcess, true);
        SetProcessId((int)processInfo.dwProcessId);

        return true;
    }

    private void SetProcessId(int processId)
    {
        Id = processId;
        _haveProcessId = true;
    }

    private static void CreatePipeWithSecurityAttributes(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize)
    {
        var ret = PInvoke.CreatePipe(out hReadPipe, out hWritePipe, lpPipeAttributes, nSize);

        if (!ret || hReadPipe.IsInvalid || hWritePipe.IsInvalid)
        {
            throw new Win32Exception();
        }
    }

    private static void CreatePipe(out SafeFileHandle parentHandle, out SafeFileHandle childHandle, bool parentInputs)
    {
        SECURITY_ATTRIBUTES securityAttributesParent = default;
        securityAttributesParent.bInheritHandle = true;

        SafeFileHandle? hTmp = null;

        try
        {
            if (parentInputs)
            {
                CreatePipeWithSecurityAttributes(out childHandle, out hTmp, ref securityAttributesParent, 0);
            }
            else
            {
                CreatePipeWithSecurityAttributes(out hTmp, out childHandle, ref securityAttributesParent, 0);
            }

            using var currentProcHandle = GetCurrentProcess_SafeHandle();

            if (!DuplicateHandle(currentProcHandle, hTmp, currentProcHandle, out parentHandle, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS))
            {
                throw new Win32Exception();
            }
        }
        finally
        {
            hTmp?.Dispose();
        }
    }

    private static string GetEnvironmentVariablesBlock(StringDictionary sd)
    {
        var keys = new string[sd.Count];
        sd.Keys.CopyTo(keys, 0);
        Array.Sort(keys, StringComparer.OrdinalIgnoreCase);

        var result = new StringBuilder(8 * keys.Length);

        foreach (var key in keys)
        {
            result.Append(key).Append('=').Append(sd[key]).Append('\0');
        }

        return result.ToString();
    }

    private static bool TryGetUserToken(uint sessionId, out SafeFileHandle hUserToken)
    {
        var userTokenHandle = default(HANDLE);
        var success = WTSQueryUserToken(sessionId, ref userTokenHandle);
        hUserToken = new SafeFileHandle(userTokenHandle, true);

        return success;
    }

    public string GetCommandLine()
    {
        if (_commandLine == null)
        {
            throw new InvalidOperationException("Process has not been started yet, or command line is unavailable.");
        }

        return _commandLine;
    }

    public bool WaitForExit(uint millisecondsTimeout = uint.MaxValue)
    {
        if (_processHandle == null || _processHandle.IsInvalid || _processHandle.IsClosed)
        {
            throw new InvalidOperationException("No process is associated with this NativeProcess object.");
        }

        var result = WaitForSingleObject(_processHandle, millisecondsTimeout);

        return result == WAIT_EVENT.WAIT_OBJECT_0;
    }

    public void Dispose()
    {
        StandardOutput?.Dispose();
        StandardError?.Dispose();
        _processHandle?.Close();
    }
}
