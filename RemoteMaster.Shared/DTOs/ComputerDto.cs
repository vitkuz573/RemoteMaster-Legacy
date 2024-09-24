// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;
using RemoteMaster.Shared.Converters;

namespace RemoteMaster.Shared.DTOs;

public class HostDto(string name, IPAddress ipAddress, PhysicalAddress macAddress) : IEquatable<HostDto>
{
    [JsonIgnore]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("ipAddress")]
    [JsonConverter(typeof(IPAddressConverter))]
    public IPAddress IpAddress { get; } = ipAddress;

    [JsonPropertyName("macAddress")]
    [JsonConverter(typeof(PhysicalAddressConverter))]
    public PhysicalAddress MacAddress { get; } = macAddress;

    [JsonIgnore]
    public byte[]? Thumbnail { get; set; }

    [JsonIgnore]
    public Guid OrganizationId { get; init; }

    [JsonIgnore]
    public Guid OrganizationalUnitId { get; init; }

    public bool Equals(HostDto? other)
    {
        if (other == null)
        {
            return false;
        }

        return IpAddress.Equals(other.IpAddress) && MacAddress.Equals(other.MacAddress) && Name == other.Name;
    }

    public override bool Equals(object? obj) => Equals(obj as HostDto);

    public override int GetHashCode() => HashCode.Combine(IpAddress, MacAddress, Name);
}
