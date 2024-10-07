// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class Host
{
    protected Host() { }

    internal Host(string name, IPAddress ipAddress, PhysicalAddress macAddress)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        MacAddress = macAddress ?? throw new ArgumentNullException(nameof(macAddress));
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public IPAddress IpAddress { get; private set; } = null!;

    public PhysicalAddress MacAddress { get; private set; } = null!;

    public Guid ParentId { get; private set; }

    public OrganizationalUnit Parent { get; private set; } = null!;

    internal void SetOrganizationalUnit(Guid organizationalUnitId)
    {
        ParentId = organizationalUnitId;
    }

    public void SetIpAddress(IPAddress ipAddress)
    {
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
    }

    public void SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
