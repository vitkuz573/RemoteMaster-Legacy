// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace RemoteMaster.Shared.Models;

public abstract class Node
{
    [JsonIgnore]
    public Guid NodeId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonIgnore]
    public Guid? ParentId { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(ParentId))]
    public Node? Parent { get; set; }

    [JsonIgnore]
    [InverseProperty(nameof(Parent))]
    public HashSet<Node> Nodes { get; } = [];
}