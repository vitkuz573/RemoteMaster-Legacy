// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JwtKeyController(IJwtSecurityService jwtSecurityService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Retrieves the public key", Description = "Retrieves the public key used for JWT.")]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 200)]
    [Produces("application/json")]
    public async Task<IActionResult> GetPublicKey()
    {
        var publicKey = await jwtSecurityService.GetPublicKeyAsync();

        if (publicKey != null)
        {
            return Ok(ApiResponse<byte[]>.Success(publicKey, "Public key retrieved successfully."));
        }

        return BadRequest(ApiResponse<byte[]>.Failure<byte[]>("Failed to retrieve public key."));
    }
}
