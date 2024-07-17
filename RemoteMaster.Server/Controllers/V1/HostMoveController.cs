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
public class HostMoveController(IHostMoveRequestService hostMoveRequestService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HostMoveRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<HostMoveRequest>), 400)]
    public async Task<IActionResult> GetHostMoveRequest([FromQuery] string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid MAC address",
                Detail = "The provided MAC address is invalid.",
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<HostMoveRequest>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var hostMoveRequest = await hostMoveRequestService.GetHostMoveRequestAsync(macAddress);

        if (hostMoveRequest != null)
        {
            var response = ApiResponse<HostMoveRequest>.Success(hostMoveRequest, "Host move request retrieved successfully.");

            return Ok(response);
        }

        var responseWithMessage = ApiResponse<HostMoveRequest>.Success(null!, "No host move request found for the provided MAC address.");

        return Ok(responseWithMessage);
    }

    [HttpPost("acknowledge")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> AcknowledgeMoveRequest([FromBody] string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid MAC address",
                Detail = "The provided MAC address is invalid.",
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<bool>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        try
        {
            await hostMoveRequestService.AcknowledgeMoveRequestAsync(macAddress);
            var response = ApiResponse<bool>.Success(true, "Host move request acknowledged successfully.");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Failed to acknowledge host move request",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<bool>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }
    }
}
