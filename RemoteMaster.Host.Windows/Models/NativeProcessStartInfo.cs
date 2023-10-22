// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Windows.Models;

public class NativeProcessStartInfo
{
    public string ApplicationName { get; set; }

    public string Arguments { get; set; }

    public int TargetSessionId { get; set; }

    public bool ForceConsoleSession { get; set; }

    public string DesktopName { get; set; }

    public bool HiddenWindow { get; set; }

    public bool UseCurrentUserToken { get; set; }

    public NativeProcessStartInfo(string applicationName, int targetSessionId)
    {
        ApplicationName = applicationName;
        TargetSessionId = targetSessionId;
    }
}