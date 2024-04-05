// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Models;

public class RefreshToken
{
    public int Id { get; set; }

    public string UserId { get; set; }

    public string Token { get; set; }

    public DateTime Expires { get; set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;

    public DateTime Created { get; set; }

    public string CreatedByIp { get; set; }

    public DateTime? Revoked { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsActive => Revoked == null && !IsExpired;

    public ApplicationUser User { get; set; }
}

