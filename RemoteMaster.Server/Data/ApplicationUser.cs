// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Data;
// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public Guid OrganizationId { get; set; }

    public virtual Organization Organization { get; set; }
}

