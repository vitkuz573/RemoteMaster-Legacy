// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HostConfigurationController(IHostConfigurationService hostConfigurationService) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateConfig([FromForm] HostConfiguration config)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (config == null || string.IsNullOrEmpty(config.Group))
        {
            return BadRequest("Invalid configuration details.");
        }

        config.Server = GetLocalIPAddress();

        byte[] configFileBytes;

        using (var memoryStream = await hostConfigurationService.GenerateConfigFileAsync(config))
        {
            configFileBytes = memoryStream.ToArray();
        }

        var contentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = "RemoteMaster.Host.json"
        };

        Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

        return File(configFileBytes, "application/octet-stream");
    }

    [HttpGet("download-host")]
    public IActionResult DownloadHost()
    {
        var networkPath = @"\\SERVER-DC02\Win\RemoteMaster\Host\RemoteMaster.Host.exe";

        var fileBytes = System.IO.File.ReadAllBytes(networkPath);

        var contentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = "RemoteMaster.Host.exe"
        };

        Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

        return File(fileBytes, "application/octet-stream");
    }

    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}
