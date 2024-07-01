// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Shared.Models;

public class Computer : INode, IEquatable<Computer>
{
    [Key]
    [JsonIgnore]
    public Guid NodeId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("ipAddress")]
    public required string IpAddress { get; set; }

    [JsonPropertyName("macAddress")]
    public required string MacAddress { get; set; }

    [JsonIgnore]
    [NotMapped]
    public byte[]? Thumbnail { get; set; }

    [JsonIgnore]
    public Guid? ParentId { get; set; }

    [ForeignKey(nameof(ParentId))]
    [JsonIgnore]
    public INode? Parent { get; set; }

    public async Task<bool> IsAvailable()
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(IpAddress, 1000);

            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(Computer? other)
    {
        if (other == null)
        {
            return false;
        }

        return IpAddress == other.IpAddress && MacAddress == other.MacAddress && Name == other.Name;
    }

    public override bool Equals(object? obj) => Equals(obj as Computer);

    public override int GetHashCode() => HashCode.Combine(IpAddress, MacAddress, Name);
}
