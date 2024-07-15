// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class HostConfigurationController(IOptions<ApplicationSettings> options) : ControllerBase
{
    private static readonly object FileLock = new();

    [HttpGet("download-host")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 404)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public IActionResult DownloadHost()
    {
        var filePath = Path.Combine(options.Value.ExecutablesRoot, "Host", "RemoteMaster.Host.exe");
        var fileName = Path.GetFileName(filePath);

        if (!System.IO.File.Exists(filePath))
        {
            var problemDetails = new ProblemDetails
            {
                Title = "File not found",
                Detail = "The requested file does not exist on the server.",
                Status = StatusCodes.Status404NotFound
            };

            var errorResponse = ApiResponse<string>.Failure(problemDetails, StatusCodes.Status404NotFound);

            return NotFound(errorResponse);
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
                var problemDetails = new ProblemDetails
                {
                    Title = "File access error",
                    Detail = $"An error occurred while accessing the file: {ex.Message}",
                    Status = StatusCodes.Status500InternalServerError
                };

                var errorResponse = ApiResponse<string>.Failure(problemDetails, StatusCodes.Status500InternalServerError);

                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        memoryStream.Position = 0;

        return File(memoryStream, "application/octet-stream", fileName);
    }

    [HttpPost("generateConfig")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<string>), 400)]
    [ProducesResponseType(typeof(ApiResponse<string>), 500)]
    public IActionResult GenerateConfig([FromBody] HostConfigurationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!ModelState.IsValid)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid request",
                Detail = "The provided request is invalid.",
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = ApiResponse<string>.Failure(problemDetails);

            return BadRequest(errorResponse);
        }

        var config = new HostConfiguration
        {
            Server = request.Server,
            Subject = new SubjectOptions
            {
                Organization = request.Organization,
                OrganizationalUnit = [request.OrganizationalUnit],
                Locality = request.Locality,
                State = request.State,
                Country = request.Country
            }
        };

        var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        return File(Encoding.UTF8.GetBytes(jsonContent), "application/json", "RemoteMaster.Host.json");
    }
}
