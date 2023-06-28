using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RemoteMaster.Server
{
    [Route("api/[controller]")]
    [ApiController]
    public class InformationController : ControllerBase
    {
        [HttpGet("message")]
        public ActionResult<string> GetMessage()
        {
            return Ok("Hello from the Information Controller!");
        }
    }
}
