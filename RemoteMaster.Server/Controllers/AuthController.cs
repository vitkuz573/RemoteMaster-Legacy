// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ITokenService tokenService) : ControllerBase
{
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var newTokens = await tokenService.RefreshTokensAsync(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString());

        if (newTokens.AccessToken == null || newTokens.RefreshToken == null)
        {
            return Unauthorized("Invalid refresh token");
        }

        return Ok(newTokens);
    }
}
