// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ITokenService tokenService) : ControllerBase
{
    [HttpPost("refresh-token")]
    [EnableRateLimiting("AuthRefreshPolicy")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(ApiResponse<string>.Failure<string>("Refresh token is required."));
        }

        var newTokens = await tokenService.RefreshTokensAsync(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString());

        if (newTokens.AccessToken == null || newTokens.RefreshToken == null)
        {
            return Unauthorized(ApiResponse<string>.Failure<string>("Invalid refresh token."));
        }

        var tokenData = new TokenResponseData
        {
            AccessToken = newTokens.AccessToken,
            RefreshToken = newTokens.RefreshToken
        };

        return Ok(ApiResponse<TokenResponseData>.Success(tokenData, "Tokens refreshed successfully."));
    }
}
