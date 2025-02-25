// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
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
public class OrganizationController(IOrganizationService organizationService) : ControllerBase
{
    [HttpGet("{name}")]
    [ProducesResponseType(typeof(ApiResponse<AddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganizationAsync([FromRoute, Required] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "The organization name is required and cannot be empty.",
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<string>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var organization = await organizationService.GetOrganizationAsync(name);

        if (organization == null)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Organization Not Found",
                Detail = $"No organization with the name '{name}' was found.",
                Status = StatusCodes.Status404NotFound
            };

            var errorResponse = ApiResponse<string>.Failure(problemDetails, StatusCodes.Status404NotFound);

            return NotFound(errorResponse);
        }

        var organizationAddress = new AddressDto(organization.Address.Locality, organization.Address.State, organization.Address.Country);

        var organizationDto = new OrganizationDto(organization.Id, organization.Name, organizationAddress);

        var response = ApiResponse<OrganizationDto>.Success(organizationDto, "Organization address retrieved successfully.");

        return Ok(response);
    }
}
