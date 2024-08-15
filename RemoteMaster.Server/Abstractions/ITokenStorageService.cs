// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Provides methods for storing and retrieving tokens.
/// </summary>
public interface ITokenStorageService
{
    /// <summary>
    /// Retrieves the access token for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the access token or null if not available.</returns>
    Task<Result<string?>> GetAccessTokenAsync(string userId);

    /// <summary>
    /// Retrieves the refresh token for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the refresh token or null if not available.</returns>
    Task<Result<string?>> GetRefreshTokenAsync(string userId);

    /// <summary>
    /// Stores the specified token data for the user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="tokenData">The token data to store.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<Result> StoreTokensAsync(string userId, TokenData tokenData);

    /// <summary>
    /// Clears the tokens for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<Result> ClearTokensAsync(string userId);
}
