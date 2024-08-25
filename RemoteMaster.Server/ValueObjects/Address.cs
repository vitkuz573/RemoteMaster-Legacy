// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;

namespace RemoteMaster.Server.ValueObjects;

public class Address(string locality, string state, string country)
{
    public string Locality { get; } = locality ?? throw new ArgumentNullException(nameof(locality));

    public string State { get; } = state ?? throw new ArgumentNullException(nameof(state));

    public string Country { get; } = IsValidCountryCode(country)
        ? country
        : throw new ArgumentException("Invalid country code", nameof(country));

    private static bool IsValidCountryCode(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode) || countryCode.Length != 2)
        {
            return false;
        }

        return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .Any(ri => ri.TwoLetterISORegionName.Equals(countryCode, StringComparison.OrdinalIgnoreCase));
    }

    public override bool Equals(object? obj) =>
        obj is Address address &&
        Locality == address.Locality &&
        State == address.State &&
        Country == address.Country;

    public override int GetHashCode() => HashCode.Combine(Locality, State, Country);
}
