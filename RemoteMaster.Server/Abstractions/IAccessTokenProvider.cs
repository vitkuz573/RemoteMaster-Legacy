﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines a contract for methods to retrieve and manage access tokens for a user.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Retrieves a valid access token for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. 
    /// The task result contains a <see cref="Result{T}"/> with the access token or null if not available.
    /// </returns>
    Task<Result<string?>> GetAccessTokenAsync(string userId);
}