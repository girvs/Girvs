using System;
using System.Threading.Tasks;
using Girvs.Application;
using Girvs.Domain.Caching;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Hdyt.SmartProducts.WebGrpcFrameWork.Service;

namespace Girvs.WebGrpcFrameWork.Services
{
    public class CacheService : Cache.CacheBase, IService
    {
        private readonly ICacheUsingManager _cacheUsingManager;

        public CacheService(ICacheUsingManager cacheUsingManager)
        {
            _cacheUsingManager = cacheUsingManager ?? throw new ArgumentNullException(nameof(cacheUsingManager));
        }

        public override async Task<ResponseCacheKeysModel> GetKeys(Empty request, ServerCallContext context)
        {
            ResponseCacheKeysModel d=new ResponseCacheKeysModel();
            var keys = await _cacheUsingManager.GetAllKeysAsync();
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
                Value = await _cacheUsingManager.GetToString(request.Key)
            };
        }

        public override async Task<Empty> RemoveByPrefix(RequestCachePrefixModel request, ServerCallContext context)
        {
            await _cacheUsingManager.ReMoveByPrefixAsync(request.Prefix);
            return new Empty();
        }

        public override async Task<Empty> RemoveByKey(RequestCacheKeyModel request, ServerCallContext context)
        {
            await _cacheUsingManager.ReMoveAsync(request.Key);
            return new Empty();
        }
    }
}