// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class JwtOptions
{
    public string KeysPath { get; init; }

    public string Issuer { get; init; }

    public string Audience { get; init; }

    public int AccessTokenExpirationMinutes { get; set; } = 120;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
