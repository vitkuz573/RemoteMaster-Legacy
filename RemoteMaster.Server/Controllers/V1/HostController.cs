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
public class HostController(IHostRegistrationService registrationService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
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

            var errorResponse = ApiResponse.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var checkResult = await registrationService.IsHostRegisteredAsync(hostConfiguration.Host.MacAddress);

        if (checkResult.IsSuccess)
        {
            var alreadyRegisteredProblemDetails = new ProblemDetails
            {
                Title = "Host already registered",
                Detail = "The host is already registered.",
                Status = StatusCodes.Status400BadRequest
            };

            var alreadyRegisteredResponse = ApiResponse.Failure(alreadyRegisteredProblemDetails);

            return BadRequest(alreadyRegisteredResponse);
        }

        var result = await registrationService.RegisterHostAsync(hostConfiguration);

        if (result.IsSuccess)
        {
            var response = ApiResponse.Success("Host registration successful.");

            return Ok(response);
        }

        var failureProblemDetails = new ProblemDetails
        {
            Title = "Host registration failed",
            Detail = result.Errors.FirstOrDefault()?.Message ?? "The registration of the host failed.",
            Status = StatusCodes.Status400BadRequest
        };

        var failureResponse = ApiResponse.Failure(failureProblemDetails);

        return BadRequest(failureResponse);
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
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

            var errorResponse = ApiResponse.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var result = await registrationService.IsHostRegisteredAsync(macAddress);

        if (result.IsSuccess)
        {
            var response = ApiResponse.Success("Host registration status retrieved.");

            return Ok(response);
        }

        var failureProblemDetails = new ProblemDetails
        {
            Title = "Failed to retrieve host registration status",
            Detail = result.Errors.FirstOrDefault()?.Message ?? "An error occurred while retrieving the host registration status.",
            Status = StatusCodes.Status400BadRequest
        };

        var failureResponse = ApiResponse.Failure(failureProblemDetails);

        return BadRequest(failureResponse);
    }

    [HttpDelete("unregister")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
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

            var errorResponse = ApiResponse.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var result = await registrationService.UnregisterHostAsync(request);

        if (result.IsSuccess)
        {
            var response = ApiResponse.Success("Host unregister successful.");

            return Ok(response);
        }

        var failureProblemDetails = new ProblemDetails
        {
            Title = "Host unregister failed",
            Detail = result.Errors.FirstOrDefault()?.Message ?? "The unregister of the host failed.",
            Status = StatusCodes.Status400BadRequest
        };

        var failureResponse = ApiResponse.Failure(failureProblemDetails);

        return BadRequest(failureResponse);
    }

    [HttpPut("update")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
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

            var errorResponse = ApiResponse.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var result = await registrationService.UpdateHostInformationAsync(request);

        if (result.IsSuccess)
        {
            var response = ApiResponse.Success("Host update successful.");

            return Ok(response);
        }

        var failureProblemDetails = new ProblemDetails
        {
            Title = "Host update failed",
            Detail = result.Errors.FirstOrDefault()?.Message ?? "The update of the host information failed.",
            Status = StatusCodes.Status400BadRequest
        };

        var failureResponse = ApiResponse.Failure(failureProblemDetails);

        return BadRequest(failureResponse);
    }
}
