using Microsoft.AspNetCore.Mvc;
using RemoteMaster.Server.Abstractions;
using System.Drawing;

namespace RemoteMaster.Server
{
    [Route("api/[controller]")]
    [ApiController]
    public class InformationController : ControllerBase
    {
        private readonly IScreenService _screenService;

        public InformationController(IScreenService screenService)
        {
            _screenService = screenService;
        }

        [HttpGet("screenSize")]
        public ActionResult<Size> GetScreenSize()
        {
            var screenSize = _screenService.GetScreenSize();

            return Ok(screenSize);
        }

        [HttpGet("virtualScreenSize")]
        public ActionResult<Size> GetVirtualScreenSize()
        {
            var screenSize = _screenService.GetVirtualScreenSize();

            return Ok(screenSize);
        }
    }
}
