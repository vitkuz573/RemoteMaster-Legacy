// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Windows.Models;

public class NativeProcessStartInfo
{
    public string ApplicationName { get; init; }

    public string Arguments { get; init; }

    public int TargetSessionId { get; init; }

    public bool ForceConsoleSession { get; init; }

    public string DesktopName { get; init; }

    public bool CreateNoWindow { get; init; }

    public bool UseCurrentUserToken { get; init; }

    public NativeProcessStartInfo(string applicationName, int targetSessionId)
    {
        ApplicationName = applicationName;
        TargetSessionId = targetSessionId;
    }
}