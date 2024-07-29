// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.DTOs;

public class CrlInfoDto
{
    public string CrlNumber { get; set; }

    public DateTimeOffset NextUpdate { get; set; }

    public string CrlHash { get; set; }
}
