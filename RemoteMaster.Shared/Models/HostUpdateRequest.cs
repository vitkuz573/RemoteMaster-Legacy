// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class HostUpdateRequest
{
    public string MacAddress { get; set; }

    public string Organization { get; set; }

#pragma warning disable CA2227
    public List<string> OrganizationalUnit { get; set; }
#pragma warning restore CA2227

    public string IpAddress { get; set; }

    public string Name { get; set; }
}