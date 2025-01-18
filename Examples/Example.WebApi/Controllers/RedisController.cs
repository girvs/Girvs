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
            var result = await locker.PerformActionWithLockAsync(
                key,
                TimeSpan.FromSeconds(8),
                () =>
                {
                    return Task.FromResult(true);
                },
                true
            );
            list.Add(result);
            Thread.Sleep(5000);
        }

        return Ok(list);
    }
}
