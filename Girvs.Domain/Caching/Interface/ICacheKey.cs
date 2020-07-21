using System;

namespace Girvs.Domain.Caching.Interface
{
    public interface ICacheKey
    {
        string GetBuildEntityKey(Guid id);
        string CacheKeyPrefix { get; }
        string CacheKeyListPrefix { get; }
        string CacheKeyListAllPrefix { get; }
        string CacheKeyListQueryPrefix { get; }
    }
}