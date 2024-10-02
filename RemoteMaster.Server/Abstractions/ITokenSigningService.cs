// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using FluentResults;

namespace RemoteMaster.Server.Abstractions;

public interface ITokenSigningService
{
    Result<string> GenerateAccessToken(List<Claim> claims);

    Result ValidateToken(string accessToken);
}
