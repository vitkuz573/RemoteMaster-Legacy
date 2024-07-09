// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
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
}