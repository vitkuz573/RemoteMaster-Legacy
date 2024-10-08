// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
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
    [ProducesResponseType(typeof(ApiResponse<HostMoveRequest>), 404)]
    [ProducesResponseType(typeof(ApiResponse<HostMoveRequest>), 400)]
    public async Task<IActionResult> GetHostMoveRequest([FromQuery] PhysicalAddress macAddress)
    {
        var hostMoveRequestResult = await hostMoveRequestService.GetHostMoveRequestAsync(macAddress);

        if (hostMoveRequestResult.IsSuccess)
        {
            if (hostMoveRequestResult.Value is not null)
            {
                var response = ApiResponse<HostMoveRequest>.Success(hostMoveRequestResult.Value, "Host move request retrieved successfully.");
                
                return Ok(response);
            }

            var notFoundProblemDetails = new ProblemDetails
            {
                Title = "Host move request not found",
                Detail = "The specified MAC address does not have any pending move requests.",
                Status = StatusCodes.Status404NotFound
            };

            return NotFound(ApiResponse<HostMoveRequest>.Failure(notFoundProblemDetails, StatusCodes.Status404NotFound));
        }

        var problemDetailsForFailure = new ProblemDetails
        {
            Title = "Failed to retrieve host move request",
            Detail = hostMoveRequestResult.Errors.FirstOrDefault()?.Message,
            Status = StatusCodes.Status400BadRequest
        };

        return BadRequest(ApiResponse<HostMoveRequest>.Failure(problemDetailsForFailure));
    }

    [HttpPost("acknowledge")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> AcknowledgeMoveRequest([FromBody] PhysicalAddress macAddress)
    {
        var result = await hostMoveRequestService.AcknowledgeMoveRequestAsync(macAddress);

        if (result.IsSuccess)
        {
            var response = ApiResponse<bool>.Success(true, "Host move request acknowledged successfully.");
            
            return Ok(response);
        }

        var problemDetailsForFailure = new ProblemDetails
        {
            Title = "Failed to acknowledge host move request",
            Detail = result.Errors.FirstOrDefault()?.Message,
            Status = StatusCodes.Status400BadRequest
        };

        var errorResponseWithDetails = ApiResponse<bool>.Failure(problemDetailsForFailure);
        
        return BadRequest(errorResponseWithDetails);
    }
}
