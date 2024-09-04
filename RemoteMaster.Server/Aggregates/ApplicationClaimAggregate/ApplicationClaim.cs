// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Aggregates.ApplicationClaimAggregate;

public class ApplicationClaim : IAggregateRoot
{
    private ApplicationClaim() { }

    public ApplicationClaim(string type, string value, string description)
    {
        ClaimType = type;
        ClaimValue = value;
        Description = description;
    }

    public int Id { get; private set; }

    public string ClaimType { get; private set; }

    public string ClaimValue { get; private set; }

    public string Description { get; private set; }
}
