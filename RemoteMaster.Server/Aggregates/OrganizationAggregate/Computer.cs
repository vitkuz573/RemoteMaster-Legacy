// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class Computer
{
    protected Computer() { }

    public Computer(string name, string ipAddress, string macAddress)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        MacAddress = macAddress ?? throw new ArgumentNullException(nameof(macAddress));
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string IpAddress { get; private set; }

    public string MacAddress { get; private set; }

    public byte[]? Thumbnail { get; private set; }

    public Guid ParentId { get; private set; }

    public OrganizationalUnit Parent { get; private set; }

    internal void SetOrganizationalUnit(Guid organizationalUnitId)
    {
        ParentId = organizationalUnitId;
    }

    public void SetThumbnail(byte[]? thumbnail)
    {
        Thumbnail = thumbnail;
    }

    public void SetIpAddress(string ipAddress)
    {
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
    }

    public void SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
