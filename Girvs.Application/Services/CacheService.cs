using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Commands;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;

namespace Girvs.Application.Services
{
    [DynamicWebApi]
    public class CacheService : ICacheService
    {
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IMediatorHandler _bus;

        public CacheService(IStaticCacheManager staticCacheManager, IMediatorHandler bus)
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        [HttpGet]
        public Task<List<string>> GetKeys()
        {
            return Task.FromResult(_staticCacheManager.GetCacheKeys());
        }

        [HttpGet("{key}")]
        public Task<string> Get(string key)
        {
            return Task.FromResult(_staticCacheManager.GetToString(key));
        }

        [HttpDelete("{prefix}")]
        public Task DeleteByPrefix(string prefix)
        {
            var removeByPrefixCommand = new RemoveByKeyCommand(prefix);
            return _bus.SendCommand(removeByPrefixCommand);
        }

        [HttpDelete("{key}")]
        public Task DeleteByKey(string key)
        {
            var removeByKeyCommand = new RemoveByKeyCommand(key);
            return _bus.SendCommand(removeByKeyCommand);
        }

        [HttpDelete]
        public Task DeleteAll()
        {
            var keys = _staticCacheManager.GetCacheKeys();
            foreach (var key in keys)
            {
                var removeByKeyCommand = new RemoveByKeyCommand(key);
                _bus.SendCommand(removeByKeyCommand);
            }
            return Task.CompletedTask;
        }
    }
}