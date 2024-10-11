// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;

namespace RemoteMaster.Shared.Models;

public class HostMoveRequest(PhysicalAddress macAddress, string organization, string[] organizationalUnit)
{
    public PhysicalAddress MacAddress { get; } = macAddress;

    public string Organization { get; set; } = organization;

    public string[] OrganizationalUnit { get; set; } = organizationalUnit;
}
