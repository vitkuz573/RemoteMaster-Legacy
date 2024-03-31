// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("crl")]
public class CrlController(ICrlService crlService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetCrl()
    {
        try
        {
            var crlData = crlService.GenerateCrl();

            return File(crlData, "application/pkix-crl", "list.crl");
        }
        catch
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
}