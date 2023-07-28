using System.Runtime.InteropServices;
using RemoteMaster.Shared.Models;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Native.Windows;

public static class SessionHelper
{
    public static unsafe List<WindowsSession> GetActiveSessions()
    {
        var sessions = new List<WindowsSession>();

        var consoleSessionId = WTSGetActiveConsoleSessionId();
        var consoleUsername = GetUsernameFromSessionId(consoleSessionId);

        if (consoleUsername is not null)
        {
            sessions.Add(new WindowsSession
            {
                Id = consoleSessionId,
                Type = SessionType.Console,
                Name = "Console",
                Username = consoleUsername,
            });
        }

        if (WTSEnumerateSessions(HANDLE.Null, 0, 1, out var sessionInfo, out var count))
        {
            var dataSize = Marshal.SizeOf<WTS_SESSION_INFOW>();

            var current = sessionInfo;

            for (var i = 0; i < count; i++)
            {
                var session = Marshal.PtrToStructure<WTS_SESSION_INFOW>((IntPtr)current);
                current += dataSize;

                if (session.State == WTS_CONNECTSTATE_CLASS.WTSActive && session.SessionId != consoleSessionId)
                {
                    var username = GetUsernameFromSessionId(session.SessionId);

                    if (username is not null)
                    {
                        sessions.Add(new WindowsSession
                        {
                            Id = session.SessionId,
                            Name = session.pWinStationName.ToString(),
                            Type = SessionType.RDP,
                            Username = username,
                        });
                    }
                }
            }
        }

        return sessions;
    }

    public static unsafe string GetUsernameFromSessionId(uint sessionId)
    {
        if (!WTSQuerySessionInformation(HANDLE.Null, sessionId, WTS_INFO_CLASS.WTSUserName, out var buffer, out var strLen) || strLen <= 1)
        {
            return string.Empty;
        }

        var username = buffer.ToString();
        WTSFreeMemory(buffer);

        return username;
    }
}