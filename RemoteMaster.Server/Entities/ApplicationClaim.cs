// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Entities;

public class ApplicationClaim
{
    public int Id { get; set; }

    public string ClaimType { get; set; }

    public string ClaimValue { get; set; }
}
