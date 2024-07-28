// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class NodeController(INodesService nodesService, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("nodes")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<INode>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    public async Task<IActionResult> GetNodes([FromQuery] Guid? organizationId = null, [FromQuery] Guid? parentId = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid user",
                Detail = "The user ID was not found.",
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(ApiResponse<IEnumerable<INode>>.Failure(problemDetails));
        }

        var user = await userManager.Users
            .Include(u => u.AccessibleOrganizations)
            .Include(u => u.AccessibleOrganizationalUnits)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "User not found",
                Detail = "The user was not found in the database.",
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(ApiResponse<IEnumerable<INode>>.Failure(problemDetails));
        }

        try
        {
            var nodes = await LoadNodes(user, organizationId, parentId);

            return Ok(ApiResponse<IEnumerable<INode>>.Success(nodes, "Nodes retrieved successfully."));
        }
        catch (Exception ex)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Error retrieving nodes",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError
            };

            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<IEnumerable<INode>>.Failure(problemDetails));
        }
    }

    private async Task<IEnumerable<INode>> LoadNodes(ApplicationUser user, Guid? organizationId = null, Guid? parentId = null)
    {
        var accessibleOrganizations = user.AccessibleOrganizations.Select(org => org.Id).ToList();
        var accessibleOrganizationalUnits = user.AccessibleOrganizationalUnits.Select(ou => ou.Id).ToList();

        var units = new List<INode>();

        if (organizationId == null)
        {
            var organizationsResult = await nodesService.GetNodesAsync<Organization>(o => accessibleOrganizations.Contains(o.Id));

            if (!organizationsResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to load organizations: {organizationsResult.Errors.FirstOrDefault()?.Message}");
            }

            var organizations = organizationsResult.Value.ToList();
            units.AddRange(organizations);

            foreach (var organization in organizations)
            {
                var organizationalUnits = (await LoadNodes(user, organization.Id)).OfType<OrganizationalUnit>().ToList();
                organization.OrganizationalUnits.Clear();

                foreach (var unit in organizationalUnits)
                {
                    organization.OrganizationalUnits.Add(unit);
                }
            }
        }
        else
        {
            var organizationalUnitsResult = await nodesService.GetNodesAsync<OrganizationalUnit>(ou =>
                ou.OrganizationId == organizationId &&
                (parentId == null || ou.ParentId == parentId) &&
                accessibleOrganizationalUnits.Contains(ou.Id));

            if (!organizationalUnitsResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to load organizational units: {organizationalUnitsResult.Errors.FirstOrDefault()?.Message}");
            }

            var organizationalUnits = organizationalUnitsResult.Value;

            var computersResult = await nodesService.GetNodesAsync<Computer>(c => c.ParentId == parentId);

            if (!computersResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to load computers: {computersResult.Errors.FirstOrDefault()?.Message}");
            }

            var computers = computersResult.Value;

            units.AddRange(organizationalUnits);
            units.AddRange(computers);

            foreach (var unit in organizationalUnits)
            {
                var childrenUnits = (await LoadNodes(user, unit.OrganizationId, unit.Id)).OfType<OrganizationalUnit>().ToList();
                var unitComputers = (await LoadNodes(user, unit.OrganizationId, unit.Id)).OfType<Computer>().ToList();

                unit.Children.Clear();

                foreach (var child in childrenUnits)
                {
                    unit.Children.Add(child);
                }

                unit.Computers.Clear();

                foreach (var computer in unitComputers)
                {
                    unit.Computers.Add(computer);
                }
            }
        }

        return units;
    }
}
