// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Native.Windows;

[SupportedOSPlatform("windows6.0.6000")]
public static class ProcessHelper
{
    public static bool OpenInteractiveProcess(string applicationName, int targetSessionId, bool forceConsoleSession, string desktopName, bool hiddenWindow, out PROCESS_INFORMATION procInfo)
    {
        procInfo = new PROCESS_INFORMATION();

        var sessionId = GetSessionId(forceConsoleSession, targetSessionId);
        var winlogonPid = GetWinlogonPidForSession(sessionId);
        var hProcess = OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS.PROCESS_ALL_ACCESS, false, winlogonPid);

        if (!OpenProcessToken(hProcess, TOKEN_ACCESS_MASK.TOKEN_DUPLICATE, out var hPToken))
        {
            return false;
        }

        if (!DuplicateTokenEx(hPToken, TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS, null, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out var hUserTokenDup))
        {
            return false;
        }

        return CreateInteractiveProcess(hUserTokenDup, applicationName, desktopName, hiddenWindow, out procInfo);
    }

    private static uint GetSessionId(bool forceConsoleSession, int? targetSessionId)
    {
        if (!forceConsoleSession)
        {
            if (!targetSessionId.HasValue)
            {
                throw new ArgumentNullException(nameof(targetSessionId), "Target session ID must be provided when forceConsoleSession is false.");
            }

            return FindTargetSessionId(targetSessionId.Value);
        }

        return WTSGetActiveConsoleSessionId();
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
        var activeSessions = SessionHelper.GetActiveSessions();
        uint lastSessionId = 0;
        var targetSessionFound = false;

        foreach (var session in activeSessions)
        {
            lastSessionId = session.Id;

            if (session.Id == targetSessionId)
            {
                targetSessionFound = true;
                break;
            }
        }

        return targetSessionFound ? (uint)targetSessionId : lastSessionId;
    }

    private static unsafe bool CreateInteractiveProcess(SafeHandle hUserTokenDup, string applicationName, string desktopName, bool hiddenWindow, out PROCESS_INFORMATION procInfo)
    {
        fixed (char* pDesktopName = $@"winsta0\{desktopName}")
        {
            var startupInfo = new STARTUPINFOW
            {
                cb = (uint)Marshal.SizeOf<STARTUPINFOW>(),
                lpDesktop = pDesktopName
            };

            var dwCreationFlags = SetCreationFlags(hiddenWindow);

            applicationName += char.MinValue;
            var appName = new Span<char>(applicationName.ToCharArray());

            return CreateProcessAsUser(hUserTokenDup, null, ref appName, null, null, false, dwCreationFlags, null, null, startupInfo, out procInfo);
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
}