// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;

public record Address(string Locality, string State, CountryCode Country)
{
    public string Locality { get; init; } = Locality ?? throw new ArgumentNullException(nameof(Locality));
    
    public string State { get; init; } = State ?? throw new ArgumentNullException(nameof(State));
    
    public CountryCode Country { get; init; } = Country ?? throw new ArgumentNullException(nameof(Country));
}
