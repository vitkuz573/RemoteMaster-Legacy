// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Models;

public class NativeProcessStartInfo : INativeProcessStartInfo
{
    public ProcessStartInfo ProcessStartInfo { get; }

    public int? TargetSessionId { get; set; }

    public bool ForceConsoleSession { get; set; } = true;

    public string DesktopName { get; set; } = "Default";

    public bool UseCurrentUserToken { get; set; }

    public NativeProcessStartInfo(string fileName, string arguments)
    {
        ProcessStartInfo = new ProcessStartInfo(fileName, arguments);
    }

    public NativeProcessStartInfo()
    {
        ProcessStartInfo = new ProcessStartInfo();
    }
}
