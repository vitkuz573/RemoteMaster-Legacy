// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate.ValueObjects;

public class Token(string value, DateTime expires, DateTime created, IPAddress createdBy)
{
    public string Value { get; } = value;

    public DateTime Expires { get; } = expires;

    public DateTime Created { get; } = created;

    public IPAddress CreatedBy { get; } = createdBy;

    public bool IsExpired => DateTime.UtcNow >= Expires;
}
