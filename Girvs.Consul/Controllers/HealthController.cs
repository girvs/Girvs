using Microsoft.AspNetCore.Mvc;

namespace Girvs.Consul.Controllers
{
    [ApiController]
    [Route("Health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("ok");
        }
    }
}