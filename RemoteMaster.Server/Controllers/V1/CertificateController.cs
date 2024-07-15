// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RemoteMaster.Server.Controllers.V1;

[ApiController]
[Route("api/[controller]")]
[ApiVersion("1.0")]
[Consumes("application/vnd.remotemaster.v1+json")]
[Produces("application/vnd.remotemaster.v1+json")]
public class CertificateController(ICaCertificateService caCertificateService, ICertificateService certificateService) : ControllerBase
{
    [HttpGet("ca")]
    [SwaggerOperation(Summary = "Retrieves the CA certificate", Description = "Retrieves the CA certificate used for host registration.")]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 200)]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 400)]
    public IActionResult GetCaCertificate()
    {
        try
        {
            var caCertificate = caCertificateService.GetCaCertificate(X509ContentType.Cert);
            var response = new ApiResponse<byte[]>(caCertificate.Export(X509ContentType.Cert), "CA certificate retrieved successfully.", StatusCodes.Status200OK);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Error retrieving CA certificate",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = new ApiResponse<byte[]>(default!, "Failed to retrieve CA certificate.", StatusCodes.Status400BadRequest);
            errorResponse.SetError(problemDetails);

            return BadRequest(errorResponse);
        }
    }

    [HttpPost("issue")]
    [SwaggerOperation(Summary = "Issues a certificate", Description = "Issues a certificate based on the provided CSR bytes.")]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 200)]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 400)]
    public IActionResult IssueCertificate([FromBody] byte[] csrBytes)
    {
        try
        {
            var certificate = certificateService.IssueCertificate(csrBytes);
            var response = new ApiResponse<byte[]>(certificate.Export(X509ContentType.Pfx), "Certificate issued successfully.", StatusCodes.Status200OK);

            return Ok(response);
        }
        catch (Exception ex)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Error issuing certificate",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            };

            var errorResponse = new ApiResponse<byte[]>(default!, "Failed to issue certificate.", StatusCodes.Status400BadRequest);
            errorResponse.SetError(problemDetails);

            return BadRequest(errorResponse);
        }
    }
}
