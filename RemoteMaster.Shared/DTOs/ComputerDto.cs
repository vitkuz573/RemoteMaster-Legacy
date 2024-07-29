// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Shared.DTOs;

public class ComputerDto(string name, string ipAddress, string macAddress) : IEquatable<ComputerDto>
{
    [JsonPropertyName("name")]
    public string Name { get; } = name;

    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; } = ipAddress;

    [JsonPropertyName("macAddress")]
    public string MacAddress { get; } = macAddress;

    public bool Equals(ComputerDto? other)
    {
        if (other == null)
        {
            return false;
        }

        return IpAddress == other.IpAddress && MacAddress == other.MacAddress && Name == other.Name;
    }

    public override bool Equals(object? obj) => Equals(obj as ComputerDto);

    public override int GetHashCode() => HashCode.Combine(IpAddress, MacAddress, Name);
}