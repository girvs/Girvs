using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Application.Cache;
using Microsoft.AspNetCore.Mvc;

namespace Girvs.WebApiFrameWork.Controllers
{
    [ApiController]
    [Route("Cache")]
    public class CacheController : ControllerBase
    {
        private readonly ICacheService _cacheService;

        public CacheController(ICacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }
        /// <summary>
        /// 获取缓存中的所有Key
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<IList<string>>> GetKeys()
        {
            // for (int i = 0; i < 10; i++)
            // {
            //     await cacheUsingManager.SetAsync(action =>
            //         {
            //             action.CacheKey = $"{SpCachingDefaults.RedisDefaultPrefix}:{i}";
            //             action.CacheTime = 30;
            //             action.UseCache = true;
            //         }, i);
            // }
            return Ok(await _cacheService.GetKeys());
        }

        /// <summary>
        /// 根据Key,获取缓存中的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpGet("{key}")]
        public async Task<ActionResult<string>> GetValueByKey(string key)
        {
            return await _cacheService.GetValueByKey(key);
        }

        /// <summary>
        /// 根据缓存的前缀,清除缓存
        /// </summary>
        /// <param name="prefixKey"></param>
        /// <returns></returns>
        [HttpDelete("MovePrefixKey/{prefixKey}")]
        public async Task<ActionResult> RemoveByPrefix(string prefixKey)
        {
            await _cacheService.RemoveByPrefix(prefixKey);
            return NoContent();
        }

        /// <summary>
        /// 根据Key,清除缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [HttpDelete("MoveKey/{key}")]
        public async Task<ActionResult> RemoveByKey(string key)
        {
            await _cacheService.RemoveByKey(key);
            return NoContent();
        }
    }
}