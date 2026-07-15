using Girvs.BusinessBasis.Repositories;
using Girvs.BusinessBasis.UoW;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Sample.ServiceA.Entities;

namespace Sample.ServiceA.Controllers;

[ApiController]
[Route("selfcheck")]
public class SelfCheckController(IDistributedCache cache) : ControllerBase
{
    /// <summary>
    /// 缓存自检：写入一个键再读回，验证 Aspire 注入的 Redis 连接串在运行时真实可用
    /// </summary>
    [HttpGet("cache")]
    public async Task<IActionResult> Cache()
    {
        var value = $"ok-{Guid.NewGuid():N}";
        await cache.SetStringAsync("selfcheck", value);
        var readBack = await cache.GetStringAsync("selfcheck");
        return Ok(new { match = readBack == value, value = readBack });
    }

    /// <summary>
    /// 数据库自检：插入一条 Product 再按 Id 查回，验证 Aspire 注入的 MySQL 连接串 + EFCore 往返真实可用
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> Db(
        [FromServices] IRepository<Product> repo,
        [FromServices] IUnitOfWork<Product> uow)
    {
        var name = $"p-{Guid.NewGuid():N}";
        var product = new Product { Id = Guid.NewGuid(), Name = name, Price = 9.9m };
        await repo.AddAsync(product);
        await uow.Commit();

        var readBack = await repo.GetByIdAsync(product.Id);
        return Ok(new { match = readBack != null && readBack.Name == name, id = product.Id });
    }
}
