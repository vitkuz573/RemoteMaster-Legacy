// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(List<Claim> claims);

    string GenerateRefreshToken(string userId, string ipAddress);

    bool IsTokenValid(string accessToken);

    bool IsRefreshTokenValid(string refreshToken);

    Task<TokenResponseData?> RefreshAccessToken(string refreshToken);

    Task RevokeRefreshTokenAsync(string refreshToken, TokenRevocationReason revocationReason);

    Task RevokeAllRefreshTokensAsync(string userId, TokenRevocationReason revocationReason);

    Task CleanUpExpiredRefreshTokens();
}
