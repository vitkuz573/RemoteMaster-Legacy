// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class CrlController(ICrlService crlService) : ControllerBase
{
    [HttpGet]
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

            return StatusCode(500, "Internal Server Error. Please try again later.");
        }
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetCrlMetadataAsync()
    {
        try
        {
            var metadata = await crlService.GetCrlMetadataAsync();

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving CRL metadata");

            return StatusCode(500, "Internal Server Error. Please try again later.");
        }
    }
}
