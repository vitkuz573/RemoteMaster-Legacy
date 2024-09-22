// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;
using RemoteMaster.Shared.Converters;

namespace RemoteMaster.Shared.Models;

public class HostUpdateRequest
{
    [JsonConverter(typeof(PhysicalAddressConverter))]
    public PhysicalAddress MacAddress { get; set; }

    public string Organization { get; set; }

#pragma warning disable CA2227
    public List<string> OrganizationalUnit { get; set; }
#pragma warning restore CA2227

    [JsonConverter(typeof(IPAddressConverter))]
    public IPAddress IpAddress { get; set; }

    public string Name { get; set; }
}