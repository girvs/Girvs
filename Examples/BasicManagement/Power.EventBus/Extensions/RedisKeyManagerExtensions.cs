using System;
using Girvs.Domain.Caching.Interface;

namespace Power.EventBus.Extensions
{
    public static class RedisKeyManagerExtensions
    {
        public static string BuilerRedisEventLockKey<TObject>(this ICacheKeyManager<TObject> manager, IntegrationEvent @event) where TObject : new()
        {
            string key = manager.CacheKeyPrefix + @event.GetType().ToString() + ":" + "ID=" + @event.Id;
            return key;
        }
    }
}