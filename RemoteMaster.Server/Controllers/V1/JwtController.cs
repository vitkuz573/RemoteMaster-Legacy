// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class JwtController(IJwtSecurityService jwtSecurityService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 200)]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 400)]
    public async Task<IActionResult> GetPublicKey()
    {
        try
        {
            var result = await jwtSecurityService.GetPublicKeyAsync();

            if (result is { IsSuccess: true, Value.Length: > 0 })
            {
                var response = ApiResponse<byte[]>.Success(result.Value, "Public key retrieved successfully.");

                return Ok(response);
            }

            var failureProblemDetails = new ProblemDetails
            {
                Title = "Failed to retrieve public key",
                Detail = "The public key could not be retrieved or is empty.",
                Status = StatusCodes.Status400BadRequest
            };

            var failureResponse = ApiResponse<byte[]>.Failure(failureProblemDetails);

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

            var errorResponse = ApiResponse<byte[]>.Failure(problemDetails, StatusCodes.Status500InternalServerError);

            return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
        }
    }
}
