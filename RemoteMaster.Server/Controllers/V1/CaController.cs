// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class CaController(ICertificateAuthorityService certificateAuthorityService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 200)]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 400)]
    public async Task<IActionResult> GetCaCertificateAsync()
    {
        var caCertificateResult = await certificateAuthorityService.GetCaCertificateAsync(X509ContentType.Cert);

        if (caCertificateResult.IsSuccess)
        {
            var caCertificate = caCertificateResult.Value;
            var response = ApiResponse<byte[]>.Success(caCertificate.Export(X509ContentType.Cert), "CA certificate retrieved successfully.");

            return Ok(response);
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Error retrieving CA certificate",
            Detail = caCertificateResult.Errors.FirstOrDefault()?.Message ?? "Unknown error",
            Status = StatusCodes.Status400BadRequest
        };

        var errorResponse = ApiResponse<byte[]>.Failure(problemDetails);

        return BadRequest(errorResponse);
    }
}
