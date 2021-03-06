﻿using System;
using Girvs.Domain.Managers;

namespace Girvs.Domain.Caching.Interface
{
    public interface ICacheKeyManager<TObject> : IManager
    {
        int CacheTime { get; }
        string CacheKeyPrefix { get; }

        string CacheKeyListPrefix { get; }

        string CacheKeyListAllPrefix { get; }

        string CacheKeyListQueryPrefix { get; }

        string BuildCacheEntityKey(object id);

        string BuildCacheEntityOtherKey(string key);
    }
}