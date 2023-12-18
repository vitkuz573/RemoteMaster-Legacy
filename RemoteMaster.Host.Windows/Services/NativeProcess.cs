// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Host.Windows.Models;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

#pragma warning disable CA2000

public class NativeProcess
{
    private SafeFileHandle? _processHandle;

    public NativeProcessStartInfo StartInfo { get; private set; }

    public uint? Id { get; private set; }

    public StreamReader? StandardOutput { get; private set; }

    public StreamReader? StandardError { get; private set; }

    public NativeProcess(NativeProcessStartInfo startInfo)
    {
        Log.Information("Initializing NativeProcess with StartInfo: {@StartInfo}", startInfo);
        StartInfo = startInfo ?? throw new ArgumentNullException(nameof(startInfo));
    }

    public static NativeProcess? Start(NativeProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        Log.Information("Starting NativeProcess with StartInfo: {@StartInfo}", startInfo);
        var sessionId = !startInfo.ForceConsoleSession ? FindTargetSessionId(startInfo.TargetSessionId) : WTSGetActiveConsoleSessionId();
        Log.Debug("Session ID determined: {SessionId}", sessionId);

        SafeFileHandle? hUserTokenDup = null;
        SafeFileHandle? stdInReadHandle = null;
        SafeFileHandle? stdOutReadHandle = null;
        SafeFileHandle? stdErrReadHandle = null;

        try
        {
            if (!startInfo.UseCurrentUserToken || !TryGetUserToken(sessionId, out hUserTokenDup))
            {
                var winlogonPid = GetWinlogonPidForSession(sessionId);
                Log.Debug("Winlogon PID for session {SessionId}: {WinlogonPid}", sessionId, winlogonPid);

                using var hProcess = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, winlogonPid);
                Log.Debug("Process handle opened: {ProcessHandle}", hProcess);

                if (!hProcess.IsInvalid && !hProcess.IsClosed && OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hPToken))
                {
                    using (hPToken)
                    {
                        DuplicateTokenEx(hPToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out hUserTokenDup);
                        Log.Debug("User token duplicated: {UserTokenDup}", hUserTokenDup);
                    }
                }
                else
                {
                    Log.Error("Failed to open or duplicate user token for session {SessionId}", sessionId);
                    return null;
                }
            }

            PROCESS_INFORMATION procInfo;
            if (hUserTokenDup != null)
            {
                if (!TryCreateInteractiveProcess(startInfo, hUserTokenDup, out procInfo, out stdOutReadHandle, out stdErrReadHandle))
                {
                    Log.Error("Failed to create interactive process.");
                    return null;
                }
            }
            else
            {
                Log.Error("User token duplication failed.");
                throw new InvalidOperationException("Failed to get or duplicate user token.");
            }

            if (stdOutReadHandle != null && stdErrReadHandle != null)
            {
                var nativeProcess = new NativeProcess(startInfo)
                {
                    _processHandle = new SafeFileHandle(procInfo.hProcess, true),
                    Id = procInfo.dwProcessId,
                    StandardOutput = new StreamReader(new FileStream(stdOutReadHandle, FileAccess.Read)),
                    StandardError = new StreamReader(new FileStream(stdErrReadHandle, FileAccess.Read))
                };

                Log.Information("NativeProcess started successfully with ID: {ProcessId}", procInfo.dwProcessId);
                return nativeProcess;
            }
            else
            {
                Log.Error("Failed to create standard output and error read handles.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while starting NativeProcess.");
            throw;
        }
    }

    private static uint GetWinlogonPidForSession(uint sessionId)
    {
        Log.Information("Retrieving Winlogon PID for session {SessionId}", sessionId);
        foreach (var process in Process.GetProcessesByName("winlogon"))
        {
            if ((uint)process.SessionId == sessionId)
            {
                Log.Information("Found Winlogon PID: {WinlogonPid} for session {SessionId}", process.Id, sessionId);
                return (uint)process.Id;
            }
        }

        Log.Error("No Winlogon process found for session {SessionId}", sessionId);
        throw new Exception("No winlogon process found for the given session id.");
    }

    private static uint FindTargetSessionId(int targetSessionId)
    {
        Log.Information("Finding target session ID: {TargetSessionId}", targetSessionId);
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

        var foundSessionId = targetSessionFound ? (uint)targetSessionId : lastSessionId;
        Log.Information("Target session ID found: {FoundSessionId}", foundSessionId);
        return foundSessionId;
    }

    private static unsafe bool TryCreateInteractiveProcess(NativeProcessStartInfo startInfo, SafeHandle hUserTokenDup, out PROCESS_INFORMATION procInfo, out SafeFileHandle stdOutReadHandle, out SafeFileHandle stdErrReadHandle)
    {
        Log.Information("Attempting to create interactive process with StartInfo: {@StartInfo}", startInfo);

        procInfo = default;
        stdOutReadHandle = null;
        stdErrReadHandle = null;
        SafeFileHandle stdOutWriteHandle = null;
        SafeFileHandle stdErrWriteHandle = null;

        if (!CreatePipe(out var stdInReadHandle, out var stdInWriteHandle, null, 0))
        {
            Log.Error("Failed to create pipe for standard input.");
            return false;
        }

        if (!CreatePipe(out stdOutReadHandle, out stdOutWriteHandle, null, 0))
        {
            Log.Error("Failed to create pipe for standard output.");
            return false;
        }

        if (!CreatePipe(out stdErrReadHandle, out stdErrWriteHandle, null, 0))
        {
            Log.Error("Failed to create pipe for standard error.");
            return false;
        }

        if (!SetHandleInformation(stdOutWriteHandle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {
            Log.Error("Failed to set inheritance attribute for the hStdOutput handle.");
            return false;
        }

        if (!SetHandleInformation(stdErrWriteHandle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {
            Log.Error("Failed to set inheritance attribute for the hStdError handle.");
            return false;
        }

        fixed (char* pDesktopName = $@"winsta0\{startInfo.DesktopName}")
        {
            var startupInfo = new STARTUPINFOW
            {
                cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                lpDesktop = pDesktopName,
                hStdInput = (HANDLE)stdInReadHandle.DangerousGetHandle(),
                hStdOutput = (HANDLE)stdOutWriteHandle.DangerousGetHandle(),
                hStdError = (HANDLE)stdErrWriteHandle.DangerousGetHandle(),
                dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES
            };

            var dwCreationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;

            dwCreationFlags |= startInfo.CreateNoWindow
                ? PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW
                : PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;

            var fullCommand = $"{startInfo.FileName} {startInfo.Arguments ?? string.Empty}";
            fullCommand += char.MinValue;

            var commandSpan = new Span<char>(fullCommand.ToCharArray());
            bool result = CreateProcessAsUser(hUserTokenDup, null, ref commandSpan, null, null, startInfo.InheritHandles, dwCreationFlags, null, null, startupInfo, out procInfo);

            if (result)
            {
                Log.Information("Interactive process created successfully. Process ID: {ProcessId}", procInfo.dwProcessId);
            }
            else
            {
                Log.Error("Failed to create interactive process.");
            }

            stdInWriteHandle.Close();

            return result;
        }
    }

    private static bool TryGetUserToken(uint sessionId, out SafeFileHandle hUserToken)
    {
        Log.Information("Attempting to get user token for session {SessionId}", sessionId);
        var userTokenHandle = default(HANDLE);
        var success = WTSQueryUserToken(sessionId, ref userTokenHandle);
        hUserToken = new SafeFileHandle(userTokenHandle, true);

        if (success)
        {
            Log.Information("User token retrieved successfully for session {SessionId}", sessionId);
        }
        else
        {
            Log.Error("Failed to get user token for session {SessionId}", sessionId);
        }

        return success;
    }

    internal static unsafe List<WTS_SESSION_INFOW> GetActiveSessions()
    {
        Log.Information("Retrieving active sessions.");
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

    public void Close()
    {
        StandardOutput?.Dispose();
        StandardError?.Dispose();
        _processHandle?.Close();
    }
}