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

    public void SetParent(OrganizationalUnit newParent)
    {
        Parent = newParent ?? throw new ArgumentNullException(nameof(newParent));
        ParentId = newParent.Id;
    }

    public void SetThumbnail(byte[]? thumbnail)
    {
        Thumbnail = thumbnail;
    }

    public void SetIpAddress(string newIpAddress)
    {
        IpAddress = newIpAddress ?? throw new ArgumentNullException(nameof(newIpAddress));
    }

    public void SetMacAddress(string newMacAddress)
    {
        MacAddress = newMacAddress ?? throw new ArgumentNullException(nameof(newMacAddress));
    }

    public void SetName(string newName)
    {
        Name = newName ?? throw new ArgumentNullException(nameof(newName));
    }
}
