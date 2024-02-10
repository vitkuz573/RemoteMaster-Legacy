// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class OrganizationalUnitChangeRequest(string macAddress, string newOrganizationalUnit)
{
    public string MacAddress { get; } = macAddress;

    public string NewOrganizationalUnit { get; set; } = newOrganizationalUnit;
}
