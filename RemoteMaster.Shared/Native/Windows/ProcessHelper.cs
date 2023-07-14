using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Native.Windows;

public static class ProcessHelper
{
    public static unsafe bool OpenInteractiveProcess(string applicationName, int targetSessionId, bool forceConsoleSession, string desktopName, bool hiddenWindow, out PROCESS_INFORMATION procInfo)
    {
        uint winlogonPid = 0;

        procInfo = new PROCESS_INFORMATION();

        var dwSessionId = GetSessionId(forceConsoleSession, targetSessionId);

        foreach (var process in Process.GetProcessesByName("winlogon"))
        {
            if ((uint)process.SessionId == dwSessionId)
            {
                winlogonPid = (uint)process.Id;
            }
        }

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

    private static uint GetSessionId(bool forceConsoleSession, int targetSessionId)
    {
        if (forceConsoleSession)
        {
            return WTSGetActiveConsoleSessionId();
        }
        else
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

            var dwCreationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT;

            if (hiddenWindow)
            {
                dwCreationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                startupInfo.wShowWindow = 0;
            }
            else
            {
                dwCreationFlags |= PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
            }

            applicationName += char.MinValue;
            var appName = new Span<char>(applicationName.ToCharArray());

            return CreateProcessAsUser(hUserTokenDup, null, ref appName, null, null, false, dwCreationFlags, null, null, startupInfo, out procInfo);
        }
    }
}
