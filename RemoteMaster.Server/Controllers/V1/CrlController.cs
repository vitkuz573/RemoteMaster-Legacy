// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
[EnableRateLimiting("CrlPolicy")]
public class CrlController(ICrlService crlService) : ControllerBase
{
    [HttpGet(Name = "GetCrl")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<IActionResult> GetCrlAsync()
    {
        var crlResult = await crlService.GenerateCrlAsync();

        if (crlResult.IsSuccess)
        {
            return File(crlResult.Value, "application/pkix-crl", "list.crl");
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Internal Server Error",
            Detail = crlResult.Errors.FirstOrDefault()?.Message ?? "An error occurred while generating the CRL. Please try again later.",
            Status = StatusCodes.Status500InternalServerError
        };

        var errorResponse = ApiResponse<string>.Failure(problemDetails, StatusCodes.Status500InternalServerError);

        return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
    }

    [HttpGet("metadata", Name = "GetCrlMetadata")]
    [ProducesResponseType(typeof(ApiResponse<CrlMetadata>), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public async Task<IActionResult> GetCrlMetadataAsync()
    {
        var metadataResult = await crlService.GetCrlMetadataAsync();

        if (metadataResult.IsSuccess)
        {
            var response = ApiResponse<CrlMetadata>.Success(metadataResult.Value, "CRL metadata retrieved successfully.");

            var selfUrl = Url.Action("GetCrlMetadata");
            var downloadUrl = Url.Action("GetCrl");

            response.SetLinks(new Dictionary<string, string>
            {
                { "self", selfUrl! },
                { "downloadCRL", downloadUrl! }
            });

            return Ok(response);
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Internal Server Error",
            Detail = metadataResult.Errors.FirstOrDefault()?.Message ?? "An error occurred while retrieving the CRL metadata. Please try again later.",
            Status = StatusCodes.Status500InternalServerError
        };

        var errorResponse = ApiResponse<string>.Failure(problemDetails, StatusCodes.Status500InternalServerError);

        return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
    }
}
