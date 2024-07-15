// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace RemoteMaster.Server.Controllers.V1;

[Route("api/[controller]")]
[ApiController]
public class HostConfigurationController(IOptions<ApplicationSettings> options) : ControllerBase
{
    private static readonly object FileLock = new();

    [HttpGet("download-host")]
    [EnableRateLimiting("HostDownloadPolicy")]
    public IActionResult DownloadHost()
    {
        var filePath = Path.Combine(options.Value.ExecutablesRoot, "Host", "RemoteMaster.Host.exe");
        var fileName = Path.GetFileName(filePath);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(ApiResponse<string>.Failure<string>("File not found.", StatusCodes.Status404NotFound));
        }

        var memoryStream = new MemoryStream();

        lock (FileLock)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                stream.CopyTo(memoryStream);
            }
            catch (IOException ex)
            {
                return StatusCode(500, ApiResponse<string>.Failure<string>($"File access error: {ex.Message}", StatusCodes.Status500InternalServerError));
            }
        }

        memoryStream.Position = 0;

        return File(memoryStream, "application/octet-stream", fileName);
    }
}
