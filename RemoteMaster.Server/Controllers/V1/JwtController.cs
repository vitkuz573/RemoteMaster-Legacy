// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class JwtController(IJwtSecurityService jwtSecurityService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Retrieves the public key", Description = "Retrieves the public key used for JWT.")]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 200)]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 400)]
    public async Task<IActionResult> GetPublicKey()
    {
        try
        {
            var publicKey = await jwtSecurityService.GetPublicKeyAsync();

            if (publicKey != null)
            {
                var response = new ApiResponse<byte[]>(publicKey, "Public key retrieved successfully.", StatusCodes.Status200OK);

                return Ok(response);
            }

            var failureResponse = new ApiResponse<byte[]>(default!, "Failed to retrieve public key.", StatusCodes.Status400BadRequest);

            return BadRequest(failureResponse);
        }
        catch (Exception ex)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Error retrieving public key",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            };

            var errorResponse = new ApiResponse<byte[]>(default, "Internal Server Error. Please try again later.", StatusCodes.Status500InternalServerError);
            errorResponse.SetError(problemDetails);

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }
}