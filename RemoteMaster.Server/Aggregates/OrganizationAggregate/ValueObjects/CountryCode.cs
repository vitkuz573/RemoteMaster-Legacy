// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;

public record CountryCode
{
    public string Code { get; }

    public CountryCode(string code)
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code), "Country code cannot be null");
        }

        if (!IsValidCountryCode(code))
        {
            throw new ArgumentException("Invalid country code", nameof(code));
        }

        Code = code;
    }

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
}
