// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate.ValueObjects;

public record Token(string Value, DateTime Expires, DateTime Created, IPAddress CreatedBy)
{
    public bool IsExpired => DateTime.UtcNow >= Expires;
}
