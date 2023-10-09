// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientConfigurationController : ControllerBase
{
    private readonly IClientConfigurationService _clientConfigurationService;

    public ClientConfigurationController(IClientConfigurationService clientConfigurationService)
    {
        _clientConfigurationService = clientConfigurationService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateConfig([FromForm] ConfigurationModel config)
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

        using (var memoryStream = await _clientConfigurationService.GenerateConfigFileAsync(config))
        {
            configFileBytes = memoryStream.ToArray();
        }

        var contentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = "RemoteMaster.Agent.json"
        };

        Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

        return File(configFileBytes, "application/octet-stream");
    }

    [HttpGet("download-agent")]
    public IActionResult DownloadAgent()
    {
        var networkPath = @"\\SERVER-DC02\Win\RemoteMaster\Agent\RemoteMaster.Agent.exe";
        var username = "support@it-ktk.local";
        var password = "bonesgamer123!!";

        NetworkDriveHelper.MapNetworkDrive(@"\\SERVER-DC02\Win\RemoteMaster", username, password);

        byte[] fileBytes;
        try
        {
            fileBytes = System.IO.File.ReadAllBytes(networkPath);
        }
        finally
        {
            NetworkDriveHelper.CancelNetworkDrive(@"\\SERVER-DC02\Win\RemoteMaster");
        }

        var contentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = "RemoteMaster.Agent.exe"
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
