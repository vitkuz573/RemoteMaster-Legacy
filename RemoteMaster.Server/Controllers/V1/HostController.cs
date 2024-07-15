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
public class HostController(IHostRegistrationService registrationService) : ControllerBase
{
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Registers a new host", Description = "Registers a new host with the given configuration.")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> RegisterHost([FromBody] HostConfiguration hostConfiguration)
    {
        var result = await registrationService.RegisterHostAsync(hostConfiguration);

        if (result)
        {
            var response = ApiResponse<bool>.Success(result, "Host registration successful.");

            return Ok(response);
        }

        var errorResponse = ApiResponse<bool>.Failure<bool>("Host registration failed.");

        return BadRequest(errorResponse);
    }

    [HttpGet("check")]
    [SwaggerOperation(Summary = "Checks if a host is registered", Description = "Checks the registration status of a host with the given MAC address.")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> CheckHostRegistration([FromQuery] string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            var errorResponse = ApiResponse<bool>.Failure<bool>("Invalid MAC address.");

            return BadRequest(errorResponse);
        }

        var isRegistered = await registrationService.IsHostRegisteredAsync(macAddress);
        var response = ApiResponse<bool>.Success(isRegistered, "Host registration status retrieved.");

        return Ok(response);
    }

    [HttpDelete("unregister")]
    [SwaggerOperation(Summary = "Unregisters a host", Description = "Unregisters a host with the given MAC address, organization details, and host name.")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> UnregisterHost([FromBody] HostUnregisterRequest request)
    {
        var result = await registrationService.UnregisterHostAsync(request);

        if (result)
        {
            var response = ApiResponse<bool>.Success(result, "Host unregistration successful.");

            return Ok(response);
        }

        var errorResponse = ApiResponse<bool>.Failure<bool>("Host unregistration failed.");

        return BadRequest(errorResponse);
    }

    [HttpPut("update")]
    [SwaggerOperation(Summary = "Updates host information", Description = "Updates the information of a host with the given configuration.")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<bool>), 400)]
    public async Task<IActionResult> UpdateHost([FromBody] HostUpdateRequest request)
    {
        var result = await registrationService.UpdateHostInformationAsync(request);

        if (result)
        {
            var response = ApiResponse<bool>.Success(result, "Host update successful.");

            return Ok(response);
        }

        var errorResponse = ApiResponse<bool>.Failure<bool>("Host update failed.");

        return BadRequest(errorResponse);
    }
}
