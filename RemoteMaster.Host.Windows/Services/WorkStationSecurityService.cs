// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.System.Shutdown;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class WorkStationSecurityService : IWorkStationSecurityService
{
    /// <summary>
    /// Locks the workstation's display.
    /// </summary>
    /// <returns>
    /// True if the lock operation was initiated successfully, false otherwise.
    /// </returns>
    public bool LockWorkStationDisplay() => LockWorkStation();

    /// <summary>
    /// Logs off the interactive user.
    /// </summary>
    /// <param name="force">If true, forces the logoff even if there are unsaved changes in open applications.</param>
    /// <returns>True if the logoff operation was initiated successfully, false otherwise.</returns>
    public bool LogOffUser(bool force)
    {
        var flags = force ? EXIT_WINDOWS_FLAGS.EWX_LOGOFF | EXIT_WINDOWS_FLAGS.EWX_FORCE : EXIT_WINDOWS_FLAGS.EWX_LOGOFF;

        return ExitWindowsEx(flags, SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OTHER);
    }
}