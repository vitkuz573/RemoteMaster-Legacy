// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate.ValueObjects;

public class TokenRevocationInfo(DateTime? revoked, IPAddress? revokedBy, TokenRevocationReason revocationReason)
{
    public DateTime? Revoked { get; } = revoked;

    public IPAddress? RevokedBy { get; } = revokedBy;

    public TokenRevocationReason RevocationReason { get; } = revocationReason;

    public bool IsRevoked => Revoked.HasValue;
}
