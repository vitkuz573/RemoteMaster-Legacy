// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;

public class Address(string locality, string state, CountryCode country)
{
    public string Locality { get; } = locality ?? throw new ArgumentNullException(nameof(locality));

    public string State { get; } = state ?? throw new ArgumentNullException(nameof(state));

    public CountryCode Country { get; } = country ?? throw new ArgumentNullException(nameof(country));

    public override bool Equals(object? obj) =>
        obj is Address address &&
        Locality == address.Locality &&
        State == address.State &&
        Country.Equals(address.Country);

    public override int GetHashCode() => HashCode.Combine(Locality, State, Country);
}
