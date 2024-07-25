// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface ITokenService
{
    /// <summary>
    /// Generates new access and refresh tokens for a user.
    /// </summary>
    /// <param name="userId">The ID of the user for whom to generate the tokens.</param>
    /// <param name="refreshToken">The existing refresh token to replace, if any.</param>
    /// <returns>A result containing the generated token data or an error message.</returns>
    Task<Result<TokenData>> GenerateTokensAsync(string userId, string? refreshToken = null);

    /// <summary>
    /// Validates the provided access token.
    /// </summary>
    /// <param name="accessToken">The access token to validate.</param>
    /// <returns>A result indicating whether the access token is valid.</returns>
    Result IsTokenValid(string accessToken);

    /// <summary>
    /// Validates the provided refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to validate.</param>
    /// <returns>A result indicating whether the refresh token is valid.</returns>
    Result IsRefreshTokenValid(string refreshToken);

    /// <summary>
    /// Revokes all active refresh tokens for a user.
    /// </summary>
    /// <param name="userId">The ID of the user whose tokens are to be revoked.</param>
    /// <param name="revocationReason">The reason for revoking the tokens.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result> RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason);

    /// <summary>
    /// Cleans up expired and revoked refresh tokens from the database.
    /// </summary>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result> CleanUpExpiredRefreshTokens();
}
