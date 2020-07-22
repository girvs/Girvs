using System;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Configuration;

namespace Girvs.Infrastructure.CacheRepository
{
    public class CacheKeyManager<TObject> : ICacheKeyManager<TObject> where TObject : new()
    {
        private readonly GirvsConfig _config;

        public CacheKeyManager(GirvsConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            CacheTime = config.CacheTime;
            CacheKeyPrefix = $"{typeof(TObject).FullName}";
            CacheKeyListPrefix = $"{CacheKeyPrefix}:List";
            CacheKeyListAllPrefix = $"{CacheKeyListPrefix}:All";
            CacheKeyListQueryPrefix = $"{CacheKeyListPrefix}:Query";
        }


        public string GetBuildEntityKey(Guid id)
        {
            return $"{CacheKeyPrefix}:{id}";
        }

        public int CacheTime { get; private set; }
        public string CacheKeyPrefix { get; private set; }
        public string CacheKeyListPrefix { get; private set; }
        public string CacheKeyListAllPrefix { get; private set; }
        public string CacheKeyListQueryPrefix { get; private set; }
    }
}