// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Windows.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.System.RemoteDesktop;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class SessionService : ISessionService
{
    public uint GetActiveConsoleSessionId()
    {
        return WTSGetActiveConsoleSessionId();
    }

    public unsafe List<WTS_SESSION_INFOW> GetActiveSessions()
    {
        var sessions = new List<WTS_SESSION_INFOW>();

        if (!WTSEnumerateSessions(HANDLE.Null, 0, 1, out var ppSessionInfo, out var count))
        {
            return sessions;
        }

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

        return sessions;
    }

    public uint FindTargetSessionId(int targetSessionId)
    {
        var activeSessions = GetActiveSessions();
        uint lastSessionId = 0;
        var targetSessionFound = false;

        foreach (var session in activeSessions)
        {
            lastSessionId = session.SessionId;

            if (session.SessionId != targetSessionId)
            {
                continue;
            }

            targetSessionFound = true;
            break;
        }

        return targetSessionFound ? (uint)targetSessionId : lastSessionId;
    }

    public uint GetProcessPid(uint sessionId, string processName)
    {
        foreach (var process in Process.GetProcessesByName(processName))
        {
            if ((uint)process.SessionId == sessionId)
            {
                return (uint)process.Id;
            }
        }

        throw new Exception($"No process named '{processName}' found for session ID {sessionId}.");
    }
}
