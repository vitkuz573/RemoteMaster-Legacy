// Copyright © 2023 Vitaly Kuzyaев. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0", Deprecated = true)]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class HostController(IHostRegistrationService registrationService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> RegisterHost([FromBody] HostConfiguration hostConfiguration)
    {
        if (!ModelState.IsValid)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid model state",
                Detail = "The provided host configuration is invalid.",
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<bool>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var result = await registrationService.RegisterHostAsync(hostConfiguration);

        if (result)
        {
            var response = ApiResponse<bool>.Success(result, "Host registration successful.");

            return Ok(response);
        }

        var failureProblemDetails = new ProblemDetails
        {
            Title = "Host registration failed",
            Detail = "The registration of the host failed.",
            Status = StatusCodes.Status400BadRequest
        };

        var failureResponse = ApiResponse<bool>.Failure(failureProblemDetails);

        return BadRequest(failureResponse);
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> CheckHostRegistration([FromQuery] string macAddress)
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

        var isRegistered = await registrationService.IsHostRegisteredAsync(macAddress);
        var response = ApiResponse<bool>.Success(isRegistered, "Host registration status retrieved.");

        return Ok(response);
    }

    [HttpDelete("unregister")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> UnregisterHost([FromBody] HostUnregisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid model state",
                Detail = "The provided request is invalid.",
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<bool>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var result = await registrationService.UnregisterHostAsync(request);

        if (result)
        {
            var response = ApiResponse<bool>.Success(result, "Host unregister successful.");

            return Ok(response);
        }

        var failureProblemDetails = new ProblemDetails
        {
            Title = "Host unregister failed",
            Detail = "The unregister of the host failed.",
            Status = StatusCodes.Status400BadRequest
        };

        var failureResponse = ApiResponse<bool>.Failure(failureProblemDetails);

        return BadRequest(failureResponse);
    }

    [HttpPut("update")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> UpdateHost([FromBody] HostUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid model state",
                Detail = "The provided host update request is invalid.",
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<bool>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var result = await registrationService.UpdateHostInformationAsync(request);

        if (result)
        {
            var response = ApiResponse<bool>.Success(result, "Host update successful.");

            return Ok(response);
        }

        var failureProblemDetails = new ProblemDetails
        {
            Title = "Host update failed",
            Detail = "The update of the host information failed.",
            Status = StatusCodes.Status400BadRequest
        };

        var failureResponse = ApiResponse<bool>.Failure(failureProblemDetails);

        return BadRequest(failureResponse);
    }
}
