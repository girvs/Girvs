using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Commands;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;

namespace Girvs.Application.Cache
{
    public class CacheService : ICacheService
    {
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IMediatorHandler _bus;

        public CacheService(IStaticCacheManager staticCacheManager, IMediatorHandler bus)
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public Task<List<string>> GetKeys()
        {
            return Task.FromResult(_staticCacheManager.GetCacheKeys());
        }

        public Task<string> GetValueByKey(string key)
        {
            return Task.FromResult(_staticCacheManager.GetToString(key));
        }

        public Task RemoveByPrefix(string prefix)
        {
            var removeByPrefixCommand = new RemoveByKeyCommand(prefix);
            return _bus.SendCommand(removeByPrefixCommand);
        }

        public Task RemoveByKey(string key)
        {
            var removeByKeyCommand = new RemoveByKeyCommand(key);
            return _bus.SendCommand(removeByKeyCommand);
        }
    }
}