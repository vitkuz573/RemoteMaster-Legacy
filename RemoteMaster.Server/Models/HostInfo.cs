// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;

namespace RemoteMaster.Server.Models;

public class HostInfo(string name, IPAddress ipAddress, PhysicalAddress macAddress)
{
    public string Name { get; } = name;

    public IPAddress IpAddress { get; } = ipAddress;

    public PhysicalAddress MacAddress { get; } = macAddress;
}
