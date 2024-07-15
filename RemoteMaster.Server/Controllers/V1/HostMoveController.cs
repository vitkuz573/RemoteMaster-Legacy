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
        var hostMoveRequest = await hostMoveRequestService.GetHostMoveRequestAsync(macAddress);

        if (hostMoveRequest != null)
        {
            return Ok(ApiResponse<HostMoveRequest>.Success(hostMoveRequest, "Host move request retrieved successfully."));
        }

        return BadRequest(ApiResponse<HostMoveRequest>.Failure<HostMoveRequest>("Failed to retrieve host move request."));
    }

    [HttpPost("acknowledge")]
    [SwaggerOperation(Summary = "Acknowledges a host move request", Description = "Acknowledges a host move request by removing it from the list.")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> AcknowledgeMoveRequest([FromBody] string macAddress)
    {
        try
        {
            await hostMoveRequestService.AcknowledgeMoveRequestAsync(macAddress);
            
            return Ok(ApiResponse<bool>.Success(true, "Host move request acknowledged successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.Failure<bool>($"Failed to acknowledge host move request: {ex.Message}"));
        }
    }
}
