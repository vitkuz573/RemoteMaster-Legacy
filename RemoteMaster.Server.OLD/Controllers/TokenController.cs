// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenController(ITokenService tokenService) : ControllerBase
{
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var newAccessToken = await tokenService.RefreshAccessToken(request.RefreshToken);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(10)
            };

            Response.Cookies.Append("accessToken", newAccessToken, cookieOptions);

            return Ok(new { AccessToken = newAccessToken });
        }
        catch (Exception ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
