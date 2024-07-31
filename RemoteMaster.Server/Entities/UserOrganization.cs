// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Entities;

public class UserOrganization
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; }

    public string UserId { get; set; }

    public ApplicationUser ApplicationUser { get; set; }
}