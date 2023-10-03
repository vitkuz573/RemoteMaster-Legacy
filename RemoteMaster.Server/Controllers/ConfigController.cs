// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguratorService _configuratorService;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(IConfiguratorService configuratorService, ILogger<ConfigController> logger)
    {
        _configuratorService = configuratorService;
        _logger = logger;

    }

    [HttpPost("generate")]
    public IActionResult GenerateConfig([FromBody] ConfigurationModel config)
    {
        _logger.LogInformation("Entered GenerateConfig method.");

        if (!ModelState.IsValid)
        {
            _logger.LogError("Model validation failed.", ModelState);

            return BadRequest(ModelState);
        }

        if (config == null || string.IsNullOrEmpty(config.Group))
        {
            return BadRequest("Invalid configuration details.");
        }

        config.Server = GetLocalIPAddress();

        byte[] configFileBytes;

        using (var memoryStream = _configuratorService.GenerateConfigFileAsync(config).Result)
        {
            configFileBytes = memoryStream.ToArray();
        }

        return Ok(new
        {
            FileName = "RemoteMaster.Agent.json",
            FileContent = Encoding.UTF8.GetString(configFileBytes)
        });
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
