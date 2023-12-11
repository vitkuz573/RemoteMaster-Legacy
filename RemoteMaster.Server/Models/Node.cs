// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations.Schema;

namespace RemoteMaster.Server.Models;

public abstract class Node
{
    public Guid NodeId { get; set; }

    public required string Name { get; set; }

    public Guid? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    public Node? Parent { get; set; }

    [InverseProperty(nameof(Parent))]
    public HashSet<Node> Nodes { get; } = [];
}