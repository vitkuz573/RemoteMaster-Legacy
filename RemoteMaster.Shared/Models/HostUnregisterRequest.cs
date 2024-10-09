// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using System.Text.Json.Serialization;
using RemoteMaster.Shared.Converters;

namespace RemoteMaster.Shared.Models;

public class HostUnregisterRequest(PhysicalAddress macAddress, string organization, List<string> organizationalUnit)
{
    [JsonConverter(typeof(PhysicalAddressConverter))]
    public PhysicalAddress MacAddress { get; } = macAddress;

    public string Organization { get; } = organization;

    public List<string> OrganizationalUnit { get; } = organizationalUnit;
}
