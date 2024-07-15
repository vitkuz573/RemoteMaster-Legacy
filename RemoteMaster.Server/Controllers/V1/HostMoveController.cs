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
public class HostMoveController(IHostMoveRequestService hostMoveRequestService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Retrieves a host move request", Description = "Retrieves a host move request by MAC address.")]
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

            var errorResponse = new ApiResponse<HostMoveRequest>(default!, "Invalid MAC address.", StatusCodes.Status400BadRequest);
            errorResponse.SetError(problemDetails);

            return BadRequest(errorResponse);
        }

        var hostMoveRequest = await hostMoveRequestService.GetHostMoveRequestAsync(macAddress);

        if (hostMoveRequest != null)
        {
            var response = new ApiResponse<HostMoveRequest>(hostMoveRequest, "Host move request retrieved successfully.", StatusCodes.Status200OK);

            return Ok(response);
        }

        var failureResponse = new ApiResponse<HostMoveRequest>(default, "Failed to retrieve host move request.", StatusCodes.Status400BadRequest);

        return BadRequest(failureResponse);
    }

    [HttpPost("acknowledge")]
    [SwaggerOperation(Summary = "Acknowledges a host move request", Description = "Acknowledges a host move request by removing it from the list.")]
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

            var errorResponse = new ApiResponse<bool>(false, "Invalid MAC address.", StatusCodes.Status400BadRequest);
            errorResponse.SetError(problemDetails);

            return BadRequest(errorResponse);
        }

        try
        {
            await hostMoveRequestService.AcknowledgeMoveRequestAsync(macAddress);
            var response = new ApiResponse<bool>(true, "Host move request acknowledged successfully.", StatusCodes.Status200OK);

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

            var errorResponse = new ApiResponse<bool>(false, $"Failed to acknowledge host move request: {ex.Message}", StatusCodes.Status400BadRequest);
            errorResponse.SetError(problemDetails);

            return BadRequest(errorResponse);
        }
    }
}
