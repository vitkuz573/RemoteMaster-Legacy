// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Host.Windows.Models;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Console;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

#pragma warning disable CA2000

namespace RemoteMaster.Host.Windows.Services;

public class NativeProcess : IDisposable
{
    private static readonly object _createProcessLock = new();

    private NativeProcessStartInfo? _startInfo;
    private SafeFileHandle? _processHandle;
    private StreamWriter? _standardInput;
    private StreamReader? _standardOutput;
    private StreamReader? _standardError;
    private uint? _processId;

    public NativeProcessStartInfo StartInfo
    {
        get
        {
            if (_startInfo == null)
            {
                _startInfo = new NativeProcessStartInfo();
            }

            return _startInfo;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _startInfo = value;
        }
    }

    public uint? Id => _processId;

    public StreamWriter? StandartInput => _standardInput;

    public StreamReader? StandardOutput => _standardOutput;

    public StreamReader? StandardError => _standardError;

    public void Start()
    {
        var sessionId = !StartInfo.ForceConsoleSession ? FindTargetSessionId(StartInfo.TargetSessionId) : WTSGetActiveConsoleSessionId();

        SafeFileHandle? hUserTokenDup = null;

        if (!StartInfo.UseCurrentUserToken || !TryGetUserToken(sessionId, out hUserTokenDup))
        {
            var winlogonPid = GetWinlogonPidForSession(sessionId);
            using var hProcess = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, winlogonPid);

            if (!hProcess.IsInvalid && !hProcess.IsClosed && OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hPToken))
            {
                using (hPToken)
                {
                    DuplicateTokenEx(hPToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup);
                }
            }
        }

        if (hUserTokenDup != null)
        {
            StartWithCreateProcess(StartInfo, hUserTokenDup);
        }
        else
        {
            throw new InvalidOperationException("Failed to get or duplicate user token.");
        }
    }

    private static uint GetWinlogonPidForSession(uint sessionId)
    {        
        foreach (var process in Process.GetProcessesByName("winlogon"))
        {
            if ((uint)process.SessionId == sessionId)
            {                
                return (uint)process.Id;
            }
        }

        throw new Exception("No winlogon process found for the given session id.");
    }

    private static uint FindTargetSessionId(int targetSessionId)
    {        
        var activeSessions = GetActiveSessions();
        uint lastSessionId = 0;
        var targetSessionFound = false;

        foreach (var session in activeSessions)
        {
            lastSessionId = session.SessionId;

            if (session.SessionId == targetSessionId)
            {
                targetSessionFound = true;
                break;
            }
        }

        return targetSessionFound ? (uint)targetSessionId : lastSessionId;
    }

    private unsafe bool StartWithCreateProcess(NativeProcessStartInfo startInfo, SafeHandle hUserTokenDup)
    {
        STARTUPINFOW startupInfo = default;
        PROCESS_INFORMATION processInfo = default;
        SECURITY_ATTRIBUTES securityAttributes = default;
        var procSH = new SafeProcessHandle();

        SafeFileHandle? parentInputPipeHandle = null;
        SafeFileHandle? childInputPipeHandle = null;
        SafeFileHandle? parentOutputPipeHandle = null;
        SafeFileHandle? childOutputPipeHandle = null;
        SafeFileHandle? parentErrorPipeHandle = null;
        SafeFileHandle? childErrorPipeHandle = null;

        lock (_createProcessLock)
        {
            try
            {
                startupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOW>();

                if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                {
                    if (startInfo.RedirectStandardInput)
                    {
                        CreatePipe(out parentInputPipeHandle, out childInputPipeHandle, true);
                    }
                    else
                    {
                        childInputPipeHandle = GetStdHandle_SafeHandle(STD_HANDLE.STD_INPUT_HANDLE);
                    }

                    if (startInfo.RedirectStandardOutput)
                    {
                        CreatePipe(out parentOutputPipeHandle, out childOutputPipeHandle, false);
                    }
                    else
                    {
                        childOutputPipeHandle = GetStdHandle_SafeHandle(STD_HANDLE.STD_OUTPUT_HANDLE);
                    }

                    if (startInfo.RedirectStandardError)
                    {
                        CreatePipe(out parentErrorPipeHandle, out childErrorPipeHandle, false);
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

                fixed (char* pDesktopName = $@"winsta0\{startInfo.DesktopName}")
                {
                    startupInfo.lpDesktop = pDesktopName;
                }

                var dwCreationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;

                dwCreationFlags |= startInfo.CreateNoWindow
                    ? PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW
                    : PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;

                var fullCommand = $"{startInfo.FileName} {startInfo.Arguments ?? string.Empty}";
                fullCommand += char.MinValue;

                var commandSpan = new Span<char>(fullCommand.ToCharArray());

                bool retVal;
                var errorCode = 0;

                retVal = CreateProcessAsUser(hUserTokenDup, null, ref commandSpan, securityAttributes, securityAttributes, true, dwCreationFlags, null, null, startupInfo, out processInfo);

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

            _standardInput = new StreamWriter(new FileStream(parentInputPipeHandle!, FileAccess.Write, 4096, false), enc, 4096)
            {
                AutoFlush = true
            };
        }
        
        if (startInfo.RedirectStandardOutput)
        {
            var enc = startInfo.StandardOutputEncoding ?? Encoding.GetEncoding((int)GetConsoleOutputCP());

            _standardOutput = new StreamReader(new FileStream(parentOutputPipeHandle!, FileAccess.Read, 4096, false), enc, true, 4096);
        }

        if (startInfo.RedirectStandardError)
        {
            var enc = startInfo.StandardErrorEncoding ?? Encoding.GetEncoding((int)GetConsoleOutputCP());

            _standardError = new StreamReader(new FileStream(parentErrorPipeHandle!, FileAccess.Read, 4096, false), enc, true, 4096);
        }

        if (procSH.IsInvalid)
        {
            procSH.Dispose();

            return false;
        }

        _processHandle = new SafeFileHandle(processInfo.hProcess, true);
        _processId = processInfo.dwProcessId;

        return true;
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
            if (hTmp != null && !hTmp.IsInvalid)
            {
                hTmp.Dispose();
            }
        }
    }

    private static bool TryGetUserToken(uint sessionId, out SafeFileHandle hUserToken)
    {        
        var userTokenHandle = default(HANDLE);
        var success = WTSQueryUserToken(sessionId, ref userTokenHandle);
        hUserToken = new SafeFileHandle(userTokenHandle, true);

        return success;
    }

    internal static unsafe List<WTS_SESSION_INFOW> GetActiveSessions()
    {
        var sessions = new List<WTS_SESSION_INFOW>();

        if (WTSEnumerateSessions(HANDLE.Null, 0, 1, out var ppSessionInfo, out var count))
        {
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var session = ppSessionInfo[i];

                    if (session.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                    {
                        sessions.Add(session);
                    }
                }
            }
            finally
            {
                WTSFreeMemory(ppSessionInfo);
            }
        }

        return sessions;
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
        _standardOutput?.Dispose();
        _standardError?.Dispose();
        _processHandle?.Close();
    }
}