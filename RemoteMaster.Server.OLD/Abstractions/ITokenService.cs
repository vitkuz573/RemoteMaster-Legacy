// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(string email);

    string GenerateRefreshToken();

    Task<bool> SaveRefreshToken(string email, string refreshToken);

    Task<string> RefreshAccessToken(string refreshToken);

    Task<bool> RevokeRefreshToken(string refreshToken);
}
