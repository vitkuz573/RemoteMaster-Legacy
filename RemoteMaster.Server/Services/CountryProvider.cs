// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class CountryProvider : ICountryProvider
{
    private readonly Lazy<List<Country>> _countries;

    public CountryProvider()
    {
        _countries = new Lazy<List<Country>>(LoadCountries);
    }

    public Result<List<Country>> GetCountries()
    {
        return Result.Ok(_countries.Value);
    }

    private List<Country> LoadCountries()
    {
        return [.. CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .DistinctBy(ri => ri.TwoLetterISORegionName)
            .Where(ri => IsValidCountryCode(ri.TwoLetterISORegionName))
            .Select(ri => new Country(ri.EnglishName, ri.TwoLetterISORegionName))
            .OrderBy(c => c.Name)];
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
