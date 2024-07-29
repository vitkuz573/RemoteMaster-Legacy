// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Entities;

public class Computer : INode
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string IpAddress { get; set; }

    public string MacAddress { get; set; }

    public byte[]? Thumbnail { get; set; }

    public Guid? ParentId { get; set; }

    public INode? Parent { get; set; }
}
