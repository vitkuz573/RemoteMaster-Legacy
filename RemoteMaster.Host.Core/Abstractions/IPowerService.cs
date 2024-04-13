// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IPowerService
{
    void Shutdown(PowerActionDto powerActionRequest);

    void Reboot(PowerActionDto powerActionRequest);
}
