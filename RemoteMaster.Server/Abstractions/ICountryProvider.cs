// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Provides country data.
/// </summary>
public interface ICountryProvider
{
    /// <summary>
    /// Gets the list of countries.
    /// </summary>
    /// <returns>A result containing a list of countries.</returns>
    Result<List<Country>> GetCountries();
}
