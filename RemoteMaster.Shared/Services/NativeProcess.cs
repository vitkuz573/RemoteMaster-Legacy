// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Shared.Models;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Services;

[SupportedOSPlatform("windows6.0.6000")]
public class NativeProcess : IDisposable
{
    public uint ProcessId { get; private set; }

    public uint ThreadId { get; private set; }

    public SafeFileHandle ProcessHandle { get; private set; }

    public SafeFileHandle ThreadHandle { get; private set; }

    public SafeFileHandle StdInReadHandle { get; private set; }

    public SafeFileHandle StdOutReadHandle { get; private set; }

    public SafeFileHandle StdErrReadHandle { get; private set; }


    public event Action<string> OutputReceived;

    public ProcessStartOptions StartOptions { get; set; }

    public NativeProcess(ProcessStartOptions options)
    {
        StartOptions = options;
    }

    public NativeProcess(PROCESS_INFORMATION procInfo, SafeFileHandle stdInReadHandle, SafeFileHandle stdOutReadHandle, SafeFileHandle stdErrReadHandle)
    {
        ProcessId = procInfo.dwProcessId;
        ThreadId = procInfo.dwThreadId;

        ProcessHandle = new SafeFileHandle(procInfo.hProcess, true);
        ThreadHandle = new SafeFileHandle(procInfo.hThread, true);

        StdInReadHandle = stdInReadHandle;
        StdOutReadHandle = stdOutReadHandle;
        StdErrReadHandle = stdErrReadHandle;
    }

    public void Start()
    {
        var proc = StartInternal(StartOptions);

        ProcessId = proc.ProcessId;
        ThreadId = proc.ThreadId;

        ProcessHandle = proc.ProcessHandle;
        ThreadHandle = proc.ThreadHandle;

        StdInReadHandle = proc.StdInReadHandle;
        StdOutReadHandle = proc.StdOutReadHandle;
        StdErrReadHandle = proc.StdErrReadHandle;
    }

    public static NativeProcess Start(ProcessStartOptions options)
    {
        var process = new NativeProcess(options);
        process.Start();

        return process;
    }

    public async Task StartListeningToOutputAsync()
    {
        if (StdOutReadHandle == null || StdOutReadHandle.IsInvalid)
        {
            throw new InvalidOperationException("Invalid standard output handle.");
        }

        if (OutputReceived == null)
        {
            throw new InvalidOperationException("No subscribers to OutputReceived event.");
        }

        using var fs = new FileStream(StdOutReadHandle, FileAccess.Read, 4096, false);
        using var reader = new StreamReader(fs, Encoding.UTF8);
        string line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            OutputReceived?.Invoke(line);
        }
    }

    private static NativeProcess StartInternal(ProcessStartOptions options)
    {
        var procInfo = new PROCESS_INFORMATION();
        var sessionId = GetSessionId(options.ForceConsoleSession, options.TargetSessionId);

        SafeFileHandle hUserTokenDup = null;
        
        SafeFileHandle stdInReadHandle = null;
        SafeFileHandle stdOutReadHandle = null;
        SafeFileHandle stdErrReadHandle = null;

        try
        {
            if (options.UseCurrentUserToken && TryGetUserToken(sessionId, out hUserTokenDup))
            {
                if (TryCreateInteractiveProcess(hUserTokenDup, options.ApplicationName, options.DesktopName, options.HiddenWindow, out procInfo, out stdInReadHandle, out stdOutReadHandle, out stdErrReadHandle))
                {
                    return new NativeProcess(procInfo, stdInReadHandle, stdOutReadHandle, stdErrReadHandle);
                }
            }
            else
            {
                var winlogonPid = GetWinlogonPidForSession(sessionId);
                using var hProcess = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, winlogonPid);
                
                if (IsProcessOpen(hProcess) && TryGetProcessToken(hProcess, out var hPToken))
                {
                    using (hPToken)
                    {
                        if (TryDuplicateToken(hPToken, out hUserTokenDup))
                        {
                            if (TryCreateInteractiveProcess(hUserTokenDup, options.ApplicationName, options.DesktopName, options.HiddenWindow, out procInfo, out stdInReadHandle, out stdOutReadHandle, out stdErrReadHandle))
                            {
                                return new NativeProcess(procInfo, stdInReadHandle, stdOutReadHandle, stdErrReadHandle);
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            hUserTokenDup?.Dispose();

            stdInReadHandle?.Dispose();
        }

        return null;
    }

    private static bool IsProcessOpen(SafeHandle hProcess) => !hProcess.IsInvalid && !hProcess.IsClosed;

    private static bool TryGetProcessToken(SafeHandle hProcess, out SafeFileHandle hPToken) => OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out hPToken);

    private static bool TryDuplicateToken(SafeHandle hPToken, out SafeFileHandle hUserTokenDup) => DuplicateTokenEx(hPToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup);

    private static uint GetSessionId(bool forceConsoleSession, int? targetSessionId)
    {
        return !forceConsoleSession
            ? FindTargetSessionId(targetSessionId.Value)
            : WTSGetActiveConsoleSessionId();
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

        return targetSessionFound
            ? (uint)targetSessionId
            : lastSessionId;
    }

    private static unsafe bool TryCreateInteractiveProcess(SafeHandle hUserTokenDup, string applicationName, string desktopName, bool hiddenWindow, out PROCESS_INFORMATION procInfo, out SafeFileHandle stdInReadHandle, out SafeFileHandle stdOutReadHandle, out SafeFileHandle stdErrReadHandle)
    {
        if (!CreatePipe(out stdInReadHandle, out var stdInWriteHandle, null, 0))
        {
            throw new Exception("Failed to create pipe for standard input.");
        }

#pragma warning disable CA2000
        if (!CreatePipe(out stdOutReadHandle, out var stdOutWriteHandle, null, 0))
        {
            throw new Exception("Failed to create pipe for standard output.");
        }

        if (!CreatePipe(out stdErrReadHandle, out var stdErrWriteHandle, null, 0))
        {
            throw new Exception("Failed to create pipe for standard error");
        }
#pragma warning restore CA2000

        const uint HANDLE_FLAG_INHERIT = 1;

        if (!SetHandleInformation(stdOutWriteHandle, HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {
            throw new Exception("Не удалось установить атрибут наследования для hStdOutput дескриптора.");
        }

        if (!SetHandleInformation(stdErrWriteHandle, HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {
            throw new Exception("Не удалось установить атрибут наследования для hStdError дескриптора.");
        }

        if (!GetHandleInformation(stdOutWriteHandle, out uint stdOutFlags) || (stdOutFlags & HANDLE_FLAG_INHERIT) != HANDLE_FLAG_INHERIT)
        {
            throw new Exception("hStdOutput дескриптор не наследуемый.");
        }

        if (!GetHandleInformation(stdErrWriteHandle, out uint stdErrFlags) || (stdErrFlags & HANDLE_FLAG_INHERIT) != HANDLE_FLAG_INHERIT)
        {
            throw new Exception("hStdError дескриптор не наследуемый.");
        }

        fixed (char* pDesktopName = $@"winsta0\{desktopName}")
        {
            var startupInfo = new STARTUPINFOW
            {
                cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                lpDesktop = pDesktopName,
                hStdOutput = (HANDLE)stdOutWriteHandle.DangerousGetHandle(),
                hStdError = (HANDLE)stdErrWriteHandle.DangerousGetHandle(),
                dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES
            };

            var dwCreationFlags = SetCreationFlags(hiddenWindow);
            applicationName += char.MinValue;
            
            var appName = new Span<char>(applicationName.ToCharArray());
            var result = CreateProcessAsUser(hUserTokenDup, null, ref appName, null, null, true, dwCreationFlags, null, null, startupInfo, out procInfo);

            stdInWriteHandle.Close();
            stdOutWriteHandle.Close();
            stdErrWriteHandle.Close();
            
            return result;
        }
    }

    private static PROCESS_CREATION_FLAGS SetCreationFlags(bool hiddenWindow)
    {
        var dwCreationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;
        
        dwCreationFlags |= hiddenWindow
            ? PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW
            : PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
        
        return dwCreationFlags;
    }

    private static bool TryGetUserToken(uint sessionId, out SafeFileHandle hUserToken)
    {
        var userTokenHandle = default(HANDLE);
        var success = WTSQueryUserToken(sessionId, ref userTokenHandle);
        hUserToken = new SafeFileHandle(userTokenHandle, true);
        
        return success;
    }

    public static unsafe List<WTS_SESSION_INFOW> GetActiveSessions()
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

    public void Dispose()
    {
        ProcessHandle?.Dispose();
        ThreadHandle?.Dispose();

        StdInReadHandle?.Dispose();
        StdOutReadHandle?.Dispose();
        StdErrReadHandle?.Dispose();
    }
}
