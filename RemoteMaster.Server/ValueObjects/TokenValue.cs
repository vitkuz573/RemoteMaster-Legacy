// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.ValueObjects;

public class TokenValue(string token, DateTime expires, DateTime created, string createdByIp)
{
    public string Token { get; } = token;

    public DateTime Expires { get; } = expires;

    public DateTime Created { get; } = created;

    public string CreatedByIp { get; } = createdByIp;

    public bool IsExpired => DateTime.UtcNow >= Expires;
}
