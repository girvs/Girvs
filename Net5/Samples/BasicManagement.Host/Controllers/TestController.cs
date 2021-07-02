using BasicManagement.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace BasicManagement.Host.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly BasicManagementDbContext _dbContext;

        public TestController(BasicManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        // GET
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("test");
        }
    }
}