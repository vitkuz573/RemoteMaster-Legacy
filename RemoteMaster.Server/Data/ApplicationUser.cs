// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Data;

public class ApplicationUser : IdentityUser
{
#pragma warning disable CA2227
    public virtual ICollection<UserOrganization> UserOrganizations { get; set; }
#pragma warning restore CA2227
}

