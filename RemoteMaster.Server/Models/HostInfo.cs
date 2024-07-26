// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Models;

public class HostInfo(string name, string ipAddress, string macAddress) : Computer(name, ipAddress, macAddress)
{
    public new HostInfo? With(string? name = null, string? ipAddress = null, string? macAddress = null)
    {
        return new HostInfo(name ?? Name, ipAddress ?? IpAddress, macAddress ?? MacAddress)
        {
            NodeId = NodeId,
            Thumbnail = Thumbnail,
            ParentId = ParentId,
            Parent = Parent
        };
    }
}
