// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class WorkStationSecurityService : IWorkStationSecurityService
{
    public bool LockWorkStationDisplay() => throw new NotImplementedException();

    public bool LogOffUser(bool force) => throw new NotImplementedException();
}
