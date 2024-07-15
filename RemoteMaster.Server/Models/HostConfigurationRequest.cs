// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class HostConfigurationRequest
{
    public string Server { get; set; }

    public string Organization { get; set; }

    public string OrganizationalUnit { get; set; }

    public string Locality { get; set; }

    public string State { get; set; }

    public string Country { get; set; }
}