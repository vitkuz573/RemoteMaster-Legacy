// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class WorkStationSecurityService : IWorkStationSecurityService
{
    public bool LockWorkStationDisplay()
    {
        try
        {
            var process = Process.Start("xdg-screensaver", "lock");

            return process.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to lock workstation display.", ex);
        }
    }

    public bool LogOffUser(bool force)
    {
        try
        {
            var command = force ? "pkill -KILL -u $(whoami)" : "pkill -u $(whoami)";
            var process = Process.Start("bash", "-c \"" + command + "\"");

            return process.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to log off user.", ex);
        }
    }
}
