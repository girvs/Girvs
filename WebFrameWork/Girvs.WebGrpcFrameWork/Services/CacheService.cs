using System;
using System.Threading.Tasks;
using Girvs.Application;
using Girvs.Application.Cache;
using Girvs.WebGrpcFrameWork.Service;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Girvs.WebGrpcFrameWork.Services
{
    public class CacheService : Cache.CacheBase, IAppGrpcService
    {
        private readonly ICacheService _cacheService;

        public CacheService(ICacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public override async Task<ResponseCacheKeysModel> GetKeys(Empty request, ServerCallContext context)
        {
            ResponseCacheKeysModel d = new ResponseCacheKeysModel();
            var keys = await _cacheService.GetKeys();
            foreach (var key in keys)
            {
                d.Key.Add(key);
            }
            return d;
        }

        public override async Task<ResponseCacheKeyValueModel> GetValueByKey(RequestCacheKeyModel request, ServerCallContext context)
        {
            return new ResponseCacheKeyValueModel
            {
                Value = await _cacheService.GetValueByKey(request.Key)
            };
        }

        public override async Task<Empty> RemoveByPrefix(RequestCachePrefixModel request, ServerCallContext context)
        {
            await _cacheService.RemoveByPrefix(request.Prefix);
            return new Empty();
        }

        public override async Task<Empty> RemoveByKey(RequestCacheKeyModel request, ServerCallContext context)
        {
            await _cacheService.RemoveByKey(request.Key);
            return new Empty();
        }
    }
}