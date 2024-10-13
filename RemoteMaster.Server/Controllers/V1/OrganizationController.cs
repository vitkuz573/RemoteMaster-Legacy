// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class OrganizationController(IApplicationUnitOfWork applicationUnitOfWork) : ControllerBase
{
    [HttpGet("address")]
    [ProducesResponseType(typeof(ApiResponse<AddressDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    public async Task<IActionResult> GetOrganizationAddress([FromQuery] string organizationName)
    {
        if (string.IsNullOrWhiteSpace(organizationName))
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "Organization name is required.",
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<string>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var organization = await applicationUnitOfWork.Organizations.FindAsync(o => o.Name == organizationName);

        var organizationEntity = organization.FirstOrDefault();

        if (organizationEntity == null)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Organization not found",
                Detail = $"Organization with name {organizationName} was not found.",
                Status = StatusCodes.Status404NotFound
            };

            var errorResponse = ApiResponse<string>.Failure(problemDetails, StatusCodes.Status404NotFound);

            return NotFound(errorResponse);
        }

        var organizationAddress = new AddressDto(organizationEntity.Address.Locality, organizationEntity.Address.State, organizationEntity.Address.Country.Code);

        var response = ApiResponse<AddressDto>.Success(organizationAddress, "Organization address retrieved successfully.");

        return Ok(response);
    }
}
