// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;

namespace RemoteMaster.Shared.Models;

public class HostUpdateRequest(PhysicalAddress macAddress, string organization, List<string> organizationalUnit, IPAddress ipAddress, string name)
{
    public PhysicalAddress MacAddress { get; } = macAddress;

    public string Organization { get; } = organization;

    public List<string> OrganizationalUnit { get; } = organizationalUnit;

    public IPAddress IpAddress { get; } = ipAddress;

    public string Name { get; } = name;
}
