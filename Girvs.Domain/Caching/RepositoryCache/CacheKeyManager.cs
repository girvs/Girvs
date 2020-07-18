using System;
using Girvs.Domain.Models;

namespace Girvs.Domain.Caching.RepositoryCache
{
    public class CacheKeyManager<Entity> : ICacheKeyManager<Entity> where Entity : BaseEntity, new()
    {
        public CacheKeyManager()
        {
            CacheKeyPrefix = $"{typeof(Entity).FullName}";
            CacheKeyListPrefix = $"{CacheKeyPrefix}:List";
            CacheKeyListAllPrefix = $"{CacheKeyListPrefix}:All";
            CacheKeyListQueryPrefix = $"{CacheKeyListPrefix}:Query";
        }

        public string CacheKeyPrefix { get; set; }
        public string CacheKeyListPrefix { get; set; }
        public string CacheKeyListAllPrefix { get; set; }
        public string CacheKeyListQueryPrefix { get; set; }
        public string CacheEntityKey(Guid id)
        {
            return $"{CacheKeyPrefix}:{id}";
        }
    }
}