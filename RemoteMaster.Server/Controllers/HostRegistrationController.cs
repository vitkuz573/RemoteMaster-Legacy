// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HostRegistrationController(IHostRegistrationService registrationService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> RegisterHost([FromBody] HostConfiguration hostConfiguration)
    {
        var result = await registrationService.RegisterHostAsync(hostConfiguration);

        if (result != null)
        {
            var response = ApiResponse<Guid?>.Success(result, "Host registration successful.");
            
            return Ok(response);
        }

        var errorResponse = ApiResponse<Guid?>.Failure<Guid?>("Host registration failed.");

        return BadRequest(errorResponse);
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckHostRegistration([FromQuery] HostConfiguration hostConfiguration)
    {
        var isRegistered = await registrationService.IsHostRegisteredAsync(hostConfiguration);
        var response = ApiResponse<bool>.Success(isRegistered, "Host registration status retrieved.");
        
        return Ok(response);
    }

    [HttpPost("unregister")]
    public async Task<IActionResult> UnregisterHost([FromBody] HostConfiguration hostConfiguration)
    {
        var result = await registrationService.UnregisterHostAsync(hostConfiguration);

        if (result)
        {
            var response = ApiResponse<bool>.Success(result, "Host unregistration successful.");
            
            return Ok(response);
        }

        var errorResponse = ApiResponse<bool>.Failure<bool>("Host unregistration failed.");
        
        return BadRequest(errorResponse);
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateHost([FromBody] HostConfiguration hostConfiguration)
    {
        var result = await registrationService.UpdateHostInformationAsync(hostConfiguration);

        if (result)
        {
            var response = ApiResponse<bool>.Success(result, "Host update successful.");

            return Ok(response);
        }

        var errorResponse = ApiResponse<bool>.Failure<bool>("Host update failed.");

        return BadRequest(errorResponse);
    }
}
