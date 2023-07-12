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

        var dwSessionId = WTSGetActiveConsoleSessionId();

        if (!forceConsoleSession)
        {
            var activeSessions = SessionHelper.GetActiveSessions();

            if (activeSessions.Any(x => x.Id == targetSessionId))
            {
                dwSessionId = (uint)targetSessionId;
            }
            else
            {
                dwSessionId = activeSessions.Last().Id;
            }
        }

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

        fixed (char* pDesktopName = @"winsta0\" + desktopName)
        {
            var startupInfo = default(STARTUPINFOW);
            startupInfo.cb = (uint)Marshal.SizeOf(startupInfo);
            startupInfo.lpDesktop = pDesktopName;

            PROCESS_CREATION_FLAGS dwCreationFlags;

            if (hiddenWindow)
            {
                dwCreationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT | PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                startupInfo.dwFlags = STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                startupInfo.wShowWindow = 0;
            }
            else
            {
                dwCreationFlags = PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS | PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT | PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
            }

            applicationName += char.MinValue;
            var appName = new Span<char>(applicationName.ToCharArray());

            return CreateProcessAsUser(hUserTokenDup, null, ref appName, null, null, false, dwCreationFlags, null, null, startupInfo, out procInfo);
        }
    }
}