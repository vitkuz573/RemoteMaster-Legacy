// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Linux.Services;

internal class PowerService : IPowerService
{
    public void Shutdown(PowerActionRequest powerActionRequest) => throw new NotImplementedException();

    public void Reboot(PowerActionRequest powerActionRequest) => throw new NotImplementedException();
}
