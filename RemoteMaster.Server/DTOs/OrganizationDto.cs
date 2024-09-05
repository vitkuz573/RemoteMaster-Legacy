// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.DTOs;

public class OrganizationDto
{
    public Guid? Id { get; set; }

    public string Name { get; set; }

    public string Locality { get; set; }

    public string State { get; set; }

    public string Country { get; set; }
}
