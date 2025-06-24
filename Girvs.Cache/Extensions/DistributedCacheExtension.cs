// using Girvs.Cache.CacheImps;
// using Microsoft.Extensions.Caching.Distributed;
//
// namespace Girvs.Cache.Extensions;
//
// public static class DistributedCacheExtension
// {
//     private static string BuildKey(string key)
//     {
//         var cacheConfig = EngineContext.Current.GetAppModuleConfig<CacheConfig>();
//         var instanceName = cacheConfig.DistributedCacheConfig.InstanceName;
//         return $"{instanceName}{key}";
//     }
//
//     public static async Task<bool> AtomicStringGetAsync(
//         this IDistributedCache distributedCache,
//         RedisConnectionWrapper redisConnectionWrapper,
//         string key
//     )
//     {
//         var dataBase = await redisConnectionWrapper.GetDatabaseAsync();
//         key = BuildKey(key);
//         await dataBase.ex(key);
//     }
//
//     public static async Task<bool> AtomicStringSetAsync(
//         this IDistributedCache distributedCache,
//         RedisConnectionWrapper redisConnectionWrapper,
//         string key,
//         TimeSpan expirationTime
//     )
//     {
//         var dataBase = await redisConnectionWrapper.GetDatabaseAsync();
//         key = BuildKey(key);
//         return await dataBase.StringSetAsync(key, key, expirationTime, When.NotExists);
//     }
//
//     public static async Task AtomicStringRemoveAsync(
//         this IDistributedCache distributedCache,
//         RedisConnectionWrapper redisConnectionWrapper,
//         string key
//     )
//     {
//         var dataBase = await redisConnectionWrapper.GetDatabaseAsync();
//         key = BuildKey(key);
//         await dataBase.KeyDeleteAsync(key);
//     }
// }
