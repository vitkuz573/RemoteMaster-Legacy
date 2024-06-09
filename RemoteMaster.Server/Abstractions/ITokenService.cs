// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

public interface ITokenService
{
    Task<TokenData> GenerateTokensAsync(string userId, string? refreshToken = null);

    bool IsTokenValid(string accessToken);

    bool IsRefreshTokenValid(string refreshToken);

    Task RevokeRefreshTokenAsync(string refreshToken, TokenRevocationReason revocationReason);

    Task RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason);

    Task CleanUpExpiredRefreshTokens();
}
