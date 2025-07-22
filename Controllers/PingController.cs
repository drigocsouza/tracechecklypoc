using Microsoft.AspNetCore.Mvc;

namespace TraceChecklyPoC.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get(bool error = false)
        {
            if (error)
            {                
                return StatusCode(500, "Simulated error for tracing");
            }
            
            return Ok("pong");
        }
    }
}