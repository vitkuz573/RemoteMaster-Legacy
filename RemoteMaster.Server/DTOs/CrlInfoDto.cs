// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.DTOs;

public class CrlInfoDto(string crlNumber, DateTimeOffset nextUpdate, string crlHash)
{
    public string CrlNumber { get; } = crlNumber;

    public DateTimeOffset NextUpdate { get; } = nextUpdate;

    public string CrlHash { get; } = crlHash;
}
