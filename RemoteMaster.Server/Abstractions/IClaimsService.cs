// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using FluentResults;
using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Abstractions;

public interface IClaimsService
{
    /// <summary>
    /// Retrieves claims for a specified user.
    /// </summary>
    /// <param name="user">The user for whom to retrieve claims.</param>
    /// <returns>A result containing the list of claims or an error message.</returns>
    Task<Result<List<Claim>>> GetClaimsForUserAsync(ApplicationUser user);
}
