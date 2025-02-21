// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
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
public class HostController(IHostRegistrationService registrationService, IHostMoveRequestService hostMoveRequestService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> RegisterHost([FromBody] HostRegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

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

        var macAddress = request.HostConfiguration.Host.MacAddress;
        var checkResult = await registrationService.IsHostRegisteredAsync(macAddress);

        if (checkResult.IsSuccess)
        {
            if (request.Force)
            {
                var hostConfig = request.HostConfiguration;
                var updateRequest = new HostUpdateRequest(hostConfig.Host.MacAddress, hostConfig.Subject.Organization, hostConfig.Subject.OrganizationalUnit, hostConfig.Host.IpAddress, hostConfig.Host.Name);

                var updateResult = await registrationService.ForceUpdateHostAsync(updateRequest);

                if (updateResult.IsSuccess)
                {
                    var response = ApiResponse.Success("Host registration updated successfully via force");

                    return Ok(response);
                }

                var updateFailure = new ProblemDetails
                {
                    Title = "Failed to update host via force",
                    Detail = updateResult.Errors.FirstOrDefault()?.Message ?? "Unknown error while updating the host.",
                    Status = StatusCodes.Status400BadRequest
                };

                return BadRequest(ApiResponse.Failure(updateFailure));
            }

            var alreadyRegisteredProblemDetails = new ProblemDetails
            {
                Title = "Host already registered",
                Detail = "The host is already registered.",
                Status = StatusCodes.Status400BadRequest
            };

            var alreadyRegisteredResponse = ApiResponse.Failure(alreadyRegisteredProblemDetails);

            return BadRequest(alreadyRegisteredResponse);
        }

        var result = await registrationService.RegisterHostAsync(request.HostConfiguration);

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

    [HttpGet("{macAddress}/status")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CheckHostRegistration([FromRoute, Required] PhysicalAddress macAddress)
    {
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

    [HttpDelete]
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

    [HttpPut]
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

    [HttpGet("{macAddress}/moveRequest")]
    [ProducesResponseType(typeof(ApiResponse<HostMoveRequest>), 200)]
    [ProducesResponseType(typeof(ApiResponse<HostMoveRequest>), 404)]
    [ProducesResponseType(typeof(ApiResponse<HostMoveRequest>), 400)]
    public async Task<IActionResult> GetHostMoveRequest([FromRoute, Required] PhysicalAddress macAddress)
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

    [HttpDelete("{macAddress}/moveRequest")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> AcknowledgeMoveRequest([FromRoute, Required] PhysicalAddress macAddress)
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
