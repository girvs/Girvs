using Girvs.Domain.Managers;
using Girvs.Domain.Models;
using System;

namespace Girvs.Domain.Caching.Interface
{
    public interface ICacheKeyManager<TEntity> : IManager where TEntity : BaseEntity
    {
        string GetBuildEntityKey(Guid id);
        string CacheKeyPrefix { get; }
        string CacheKeyListPrefix { get; }
        string CacheKeyListAllPrefix { get; }
        string CacheKeyListQueryPrefix { get; }
    }
}