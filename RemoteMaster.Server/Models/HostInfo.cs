// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;

namespace RemoteMaster.Server.Models;

public class HostInfo
{
    public string Name { get; set; }

    public IPAddress IpAddress { get; set; }

    public PhysicalAddress MacAddress { get; set; }
}
