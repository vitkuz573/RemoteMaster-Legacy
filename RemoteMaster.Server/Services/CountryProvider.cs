// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Globalization;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

/// <summary>
/// Provides country information based on specific cultures.
/// </summary>
public class CountryProvider : ICountryProvider
{
    /// <summary>
    /// Holds a statically loaded list of countries.
    /// </summary>
    private static readonly List<Country> Countries = LoadCountries();

    /// <summary>
    /// Returns a list of all available countries.
    /// </summary>
    /// <returns>
    /// A <see cref="Result{TValue}"/> containing a list of <see cref="Country"/>.
    /// </returns>
    public Result<List<Country>> GetCountries()
    {
        return Result.Ok(Countries);
    }

    /// <summary>
    /// Loads and returns a list of distinct countries derived from specific cultures.
    /// </summary>
    /// <remarks>
    /// This method retrieves all specific cultures using <see cref="CultureInfo.GetCultures(CultureTypes)"/>,
    /// converts them to <see cref="RegionInfo"/> objects, filters out duplicates, and then sorts the result by name.
    /// </remarks>
    /// <returns>
    /// A list of unique <see cref="Country"/> instances.
    /// </returns>
    private static List<Country> LoadCountries()
    {
        return [.. CultureInfo
            .GetCultures(CultureTypes.SpecificCultures)
            .Select(culture =>
            {
                try
                {
                    return new RegionInfo(culture.Name);
                }
                catch
                {
                    // Some culture names might fail to initialize RegionInfo
                    return null;
                }
            })
            .Where(regionInfo => regionInfo != null)
            .DistinctBy(regionInfo => regionInfo!.TwoLetterISORegionName)
            .Select(regionInfo => new Country(
                regionInfo!.EnglishName,
                regionInfo.TwoLetterISORegionName))
            .OrderBy(country => country.Name)];
    }
}
