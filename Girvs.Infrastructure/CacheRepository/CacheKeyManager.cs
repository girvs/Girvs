using System;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Models;

namespace Girvs.Infrastructure.CacheRepository
{
    public class CacheKeyManager<TEntity> : ICacheKeyManager<TEntity> where TEntity : BaseEntity
    {
        public CacheKeyManager()
        {
            CacheKeyPrefix = $"{typeof(TEntity).FullName}";
            CacheKeyListPrefix = $"{CacheKeyPrefix}:List";
            CacheKeyListAllPrefix = $"{CacheKeyListPrefix}:All";
            CacheKeyListQueryPrefix = $"{CacheKeyListPrefix}:Query";
        }

        public string GetBuildEntityKey(Guid id)
        {
            return $"{CacheKeyPrefix}:{id}";
        }

        public string CacheKeyPrefix { get; }
        public string CacheKeyListPrefix { get; }
        public string CacheKeyListAllPrefix { get; }
        public string CacheKeyListQueryPrefix { get; }
    }
}