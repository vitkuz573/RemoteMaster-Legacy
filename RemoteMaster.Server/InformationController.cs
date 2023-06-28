using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using System;
using System.Drawing;

namespace RemoteMaster.Server;

[Route("api/[controller]")]
[ApiController]
public class InformationController : ControllerBase
{
    private readonly IScreenService _screenService;
    private readonly ILogger<InformationController> _logger;

    public InformationController(IScreenService screenService, ILogger<InformationController> logger)
    {
        _screenService = screenService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the screen size based on type.
    /// </summary>
    /// <param name="type">The type of the screen size (physical or virtual).</param>
    /// <returns>The size of the screen.</returns>
    [HttpGet("screenSize/{type}")]
    public ActionResult<Size> GetScreenSize(string type)
    {
        try
        {
            Size screenSize;

            switch (type.ToLower())
            {
                case "physical":
                    screenSize = _screenService.GetScreenSize();
                    _logger.LogInformation("Retrieved physical screen size: {Size}", screenSize);
                    break;
                case "virtual":
                    screenSize = _screenService.GetVirtualScreenSize();
                    _logger.LogInformation("Retrieved virtual screen size: {Size}", screenSize);
                    break;
                default:
                    return BadRequest("Invalid type. Valid types are 'physical' or 'virtual'");
            }

            return Ok(screenSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {type} screen size", type);

            return StatusCode(500, $"Error retrieving {type} screen size");
        }
    }
}
