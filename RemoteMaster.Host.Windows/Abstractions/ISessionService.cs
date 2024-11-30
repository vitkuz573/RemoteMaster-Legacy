// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Windows.Win32.System.RemoteDesktop;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface ISessionService
{
    uint GetActiveConsoleSessionId();

    List<WTS_SESSION_INFOW> GetActiveSessions();

    uint FindTargetSessionId(int targetSessionId);

    uint GetProcessId(uint sessionId, string processName);
}
