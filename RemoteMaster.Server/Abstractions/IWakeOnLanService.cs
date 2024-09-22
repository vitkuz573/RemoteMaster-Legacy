// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;

namespace RemoteMaster.Server.Abstractions;

public interface IWakeOnLanService
{
    void WakeUp(PhysicalAddress macAddress, int port = 9);
}
