// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaCertificateController(ICaCertificateService caCertificateService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Retrieves the CA certificate", Description = "Retrieves the CA certificate used for host registration.")]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 200)]
    [Produces("application/json")]
    public IActionResult GetCaCertificate()
    {
        try
        {
            var caCertificate = caCertificateService.GetCaCertificate(X509ContentType.Cert);
            
            return Ok(ApiResponse<byte[]>.Success(caCertificate.Export(X509ContentType.Cert), "CA certificate retrieved successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<byte[]>.Failure<byte[]>(ex.Message));
        }
    }
}

