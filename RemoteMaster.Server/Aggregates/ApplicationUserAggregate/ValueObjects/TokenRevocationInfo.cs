// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate.ValueObjects;

public record TokenRevocationInfo(DateTime? Revoked, IPAddress? RevokedBy, TokenRevocationReason RevocationReason)
{
    public bool IsRevoked => Revoked.HasValue;
}
