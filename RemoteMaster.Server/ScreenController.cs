using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using System.Drawing;

namespace RemoteMaster.Server;

[Route("api/[controller]")]
[ApiController]
[ProducesResponseType(typeof(Size), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class ScreenController : ControllerBase
{
    private readonly IScreenService _screenService;
    private readonly ILogger<ScreenController> _logger;

    public enum ScreenType
    {
        Physical,
        Virtual
    }

    public ScreenController(IScreenService screenService, ILogger<ScreenController> logger)
    {
        _screenService = screenService ?? throw new ArgumentNullException(nameof(screenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the screen size based on the specified screen type.
    /// </summary>
    /// <param name="type">The type of the screen.</param>
    [HttpGet("size/{type}")]
    [Produces("application/json")]
    public ActionResult<Size> GetScreenSize([FromRoute] ScreenType type)
    {
        try
        {
            var screenSize = type switch
            {
                ScreenType.Physical => _screenService.GetScreenSize(),
                ScreenType.Virtual => _screenService.GetVirtualScreenSize(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), "Invalid screen type.")
            };

            _logger.LogInformation("Retrieved screen size: {Type}, {Size}", type, screenSize);

            return Ok(screenSize);
        }
        catch (ArgumentOutOfRangeException)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Invalid screen type",
                Detail = $"Invalid screen type: {type}",
                Status = StatusCodes.Status400BadRequest,
            };
            _logger.LogError("{Title}: {Type}", problemDetails.Title, type);

            return BadRequest(problemDetails);
        }
        catch (Exception ex)
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Error retrieving screen size",
                Detail = $"Error retrieving screen size: {ex.Message}",
                Status = StatusCodes.Status500InternalServerError,
            };
            _logger.LogError(ex, "{Title}: {Type}", problemDetails.Title, type);

            return StatusCode(problemDetails.Status.Value, problemDetails);
        }
    }
}
