using Girvs.Domain.Managers;
using Girvs.Domain.Models;
using System;

namespace Girvs.Domain.Caching.RepositoryCache
{
    public interface ICacheKeyManager<Entity> : IManager where Entity : BaseEntity, new()
    {
        string CacheKeyPrefix { get; set; }

        string CacheKeyListPrefix { get; set; }

        string CacheKeyListAllPrefix { get; set; }

        string CacheKeyListQueryPrefix { get; set; }

        string CacheEntityKey(Guid id);
    }
}