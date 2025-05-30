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
        var list = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            var cacheKey = new CacheKey(key + i);

            await cacheManager.SetAsync(cacheKey, i);
        }

        return Ok(list);
    }
}

