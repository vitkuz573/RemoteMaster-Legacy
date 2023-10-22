// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Host.Windows.Models;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.RemoteDesktop;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class NativeProcess
{
    public NativeProcessStartInfo StartInfo { get; set; }

    public static NativeProcess Start(NativeProcessStartInfo startInfo)
    {
        if (startInfo == null)
        {
            throw new ArgumentNullException(nameof(startInfo));
        }

        return StartInternal(startInfo);
    }

    private static NativeProcess StartInternal(NativeProcessStartInfo startInfo)
    {
        var procInfo = new PROCESS_INFORMATION();

        var sessionId = !startInfo.ForceConsoleSession
            ? FindTargetSessionId(startInfo.TargetSessionId)
            : WTSGetActiveConsoleSessionId();

        SafeFileHandle hUserTokenDup = null;
        SafeFileHandle stdInReadHandle = null;
        SafeFileHandle stdOutReadHandle = null;
        SafeFileHandle stdErrReadHandle = null;

        try
        {
            if (!startInfo.UseCurrentUserToken || !TryGetUserToken(sessionId, out hUserTokenDup))
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
                TryCreateInteractiveProcess(startInfo, hUserTokenDup, out procInfo);
            }
        }
        finally
        {
            hUserTokenDup?.Dispose();
            stdInReadHandle?.Dispose();
        }

        return null;
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

    private static unsafe bool TryCreateInteractiveProcess(NativeProcessStartInfo startInfo, SafeHandle hUserTokenDup, out PROCESS_INFORMATION procInfo)
    {
#pragma warning disable CA2000
        if (!CreatePipe(out var stdInReadHandle, out var stdInWriteHandle, null, 0))
        {
            throw new Exception("Failed to create pipe for standard input.");
        }

        if (!CreatePipe(out var stdOutReadHandle, out var stdOutWriteHandle, null, 0))
        {
            throw new Exception("Failed to create pipe for standard output.");
        }

        if (!CreatePipe(out var stdErrReadHandle, out var stdErrWriteHandle, null, 0))
        {
            throw new Exception("Failed to create pipe for standard error");
        }
#pragma warning restore CA2000

        if (!SetHandleInformation(stdOutWriteHandle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {
            throw new Exception("Failed to set inheritance attribute for the hStdOutput handle.");
        }

        if (!SetHandleInformation(stdErrWriteHandle, (uint)HANDLE_FLAGS.HANDLE_FLAG_INHERIT, HANDLE_FLAGS.HANDLE_FLAG_INHERIT))
        {
            throw new Exception("Failed to set inheritance attribute for the hStdError handle.");
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

            var fullCommand = $"{startInfo.ApplicationName} {startInfo.Arguments ?? string.Empty}";
            fullCommand += char.MinValue;
            
            var commandSpan = new Span<char>(fullCommand.ToCharArray());
            var result = CreateProcessAsUser(hUserTokenDup, null, ref commandSpan, null, null, false, dwCreationFlags, null, null, startupInfo, out procInfo);

            stdInWriteHandle.Close();

            return result;
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
}
