// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;
using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("CrlPolicy")]
public class CrlController(ICrlService crlService) : ControllerBase
{
    [HttpGet(Name = "GetCrl")]
    public async Task<IActionResult> GetCrlAsync()
    {
        try
        {
            var crlData = await crlService.GenerateCrlAsync();

            return File(crlData, "application/pkix-crl", "list.crl");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating CRL");

            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<byte[]>.Failure<string>("Internal Server Error. Please try again later.", StatusCodes.Status500InternalServerError));
        }
    }

    [HttpGet("metadata", Name = "GetCrlMetadata")]
    public async Task<IActionResult> GetCrlMetadataAsync()
    {
        try
        {
            var metadata = await crlService.GetCrlMetadataAsync();
            var response = ApiResponse<CrlMetadata>.Success(metadata);

            var selfUrl = Url.Action("GetCrlMetadata");
            var downloadUrl = Url.Action("GetCrl");

            response.SetLinks(new Dictionary<string, string>
            {
                { "self", selfUrl },
                { "downloadCRL", downloadUrl }
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving CRL metadata");

            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<CrlMetadata>.Failure<string>("Internal Server Error. Please try again later.", StatusCodes.Status500InternalServerError));
        }
    }
}

