// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;

namespace RemoteMaster.Server.Models;

public class RefreshToken
{
    public int Id { get; set; }

    public string Token { get; set; }

    public DateTime ExpiryDate { get; set; }

    public string UserId { get; set; }

    public virtual IdentityUser User { get; set; }
}
