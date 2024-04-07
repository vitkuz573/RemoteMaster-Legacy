// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HostConfigurationController(IOptions<ApplicationSettings> options) : ControllerBase
{
    [HttpGet("download-host")]
    public async Task<IActionResult> DownloadHost()
    {
        var filePath = Path.Combine(options.Value.ExecutablesRoot, "Host", "RemoteMaster.Host.exe");
        var fileName = Path.GetFileName(filePath);

        if (!System.IO.File.Exists(filePath))
        {
            var apiResponse = new ApiResponse(false, "File not found.");
            
            return NotFound(apiResponse);
        }

        var memoryStream = new MemoryStream();
        
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memoryStream);
        }
        
        memoryStream.Position = 0;

        return File(memoryStream, "application/octet-stream", fileName);
    }
}
