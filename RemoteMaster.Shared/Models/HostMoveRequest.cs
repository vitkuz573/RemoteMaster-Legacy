// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using System.Text.Json.Serialization;
using RemoteMaster.Shared.Converters;

namespace RemoteMaster.Shared.Models;

public class HostMoveRequest(PhysicalAddress macAddress, string organization, string[] organizationalUnit)
{
    [JsonPropertyName("macAddress")]
    [JsonConverter(typeof(PhysicalAddressConverter))]
    public PhysicalAddress MacAddress { get; } = macAddress;

    [JsonPropertyName("organization")]
    public string Organization { get; set; } = organization;

    [JsonPropertyName("organizationalUnit")]
    public string[] OrganizationalUnit { get; set; } = organizationalUnit;
}
