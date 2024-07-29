// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Entities;

public class Computer(string name, string ipAddress, string macAddress) : INode
{
    public Guid Id { get; set; }

    public string Name { get; } = name;

    public string IpAddress { get; } = ipAddress;

    public string MacAddress { get; } = macAddress;

    public byte[]? Thumbnail { get; set; }

    public Guid? ParentId { get; set; }

    public INode? Parent { get; set; }

    public Computer With(string? name = null, string? ipAddress = null, string? macAddress = null)
    {
        return new Computer(name ?? Name, ipAddress ?? IpAddress, macAddress ?? MacAddress)
        {
            Id = Id,
            Thumbnail = Thumbnail,
            ParentId = ParentId,
            Parent = Parent
        };
    }
}
