// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Shared.Models;

public class Computer(string name, string ipAddress, string macAddress) : INode, IEquatable<Computer>
{
    [JsonIgnore]
    public Guid NodeId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; } = ipAddress;

    [JsonPropertyName("macAddress")]
    public string MacAddress { get; } = macAddress;

    [JsonIgnore]
    public byte[]? Thumbnail { get; set; }

    [JsonIgnore]
    public Guid? ParentId { get; set; }

    [JsonIgnore]
    public INode? Parent { get; set; }

    public bool Equals(Computer? other)
    {
        if (other == null)
        {
            return false;
        }

        return IpAddress == other.IpAddress && MacAddress == other.MacAddress && Name == other.Name;
    }

    public Computer With(string? name = null, string? ipAddress = null, string? macAddress = null)
    {
        return new Computer(name ?? Name, ipAddress ?? IpAddress, macAddress ?? MacAddress)
        {
            NodeId = NodeId,
            Thumbnail = Thumbnail,
            ParentId = ParentId,
            Parent = Parent
        };
    }

    public override bool Equals(object? obj) => Equals(obj as Computer);

    public override int GetHashCode() => HashCode.Combine(IpAddress, MacAddress, Name);
}
