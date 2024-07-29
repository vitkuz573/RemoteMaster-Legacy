// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Shared.DTOs;

public class ComputerDto(string name, string ipAddress, string macAddress)
{
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
    public ComputerDto? Parent { get; set; }
}