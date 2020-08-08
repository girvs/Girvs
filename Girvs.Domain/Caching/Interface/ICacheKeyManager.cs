using System;
using Girvs.Domain.Managers;

namespace Girvs.Domain.Caching.Interface
{
    public interface ICacheKeyManager<TObject> : IManager where TObject : new()
    {
        int CacheTime { get; }
        string CacheKeyPrefix { get; }

        string CacheKeyListPrefix { get; }

        string CacheKeyListAllPrefix { get; }

        string CacheKeyListQueryPrefix { get; }

        string BuildCacheEntityKey(Guid id);

        string BuildCacheEntityOtherKey(string key);
    }
}