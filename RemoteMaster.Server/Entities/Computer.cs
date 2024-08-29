// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;

namespace RemoteMaster.Server.Entities;

public class Computer
{
    protected Computer() { }

    public Computer(string name, string ipAddress, string macAddress, OrganizationalUnit parent)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        MacAddress = macAddress ?? throw new ArgumentNullException(nameof(macAddress));
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        ParentId = parent.Id;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string IpAddress { get; private set; }

    public string MacAddress { get; private set; }

    public byte[]? Thumbnail { get; private set; }

    public Guid ParentId { get; private set; }

    public OrganizationalUnit Parent { get; private set; }

    public void SetOrganizationalUnit(OrganizationalUnit organizationalUnit)
    {
        Parent = organizationalUnit ?? throw new ArgumentNullException(nameof(organizationalUnit));
        ParentId = organizationalUnit.Id;
    }

    public void SetThumbnail(byte[]? thumbnail)
    {
        Thumbnail = thumbnail;
    }

    public void SetIpAddress(string ipAddress)
    {
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
    }

    public void SetMacAddress(string macAddress)
    {
        MacAddress = macAddress ?? throw new ArgumentNullException(nameof(macAddress));
    }

    public void SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
