// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Host.Linux.Services;

public class HardwareService : IHardwareService
{
    public void SetMonitorState(MonitorState state) => throw new NotImplementedException();
}
