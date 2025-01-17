using Girvs.AuthorizePermission;
using Girvs.Cache.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Example.WebApi.Controllers;

[Authorize(AuthenticationSchemes = GirvsAuthenticationScheme.GirvsJwt)]
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class RedisController(IStaticCacheManager cacheManager, ILocker locker) : ControllerBase
{
    [HttpGet("GetRedisLock")]
    public async Task<IActionResult> GetRedisLock(string key)
    {
        var result = await locker.PerformActionWithLockAsync(
            key,
            TimeSpan.FromSeconds(3),
            () => Task.FromResult(true)
        );
        return Ok(result);
    }
}
