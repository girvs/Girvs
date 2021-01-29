using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs.Application.Dtos;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Enumerations;
using Girvs.Domain.GirvsAuthorizePermission;
using Microsoft.AspNetCore.Authorization;
using Panda.DynamicWebApi.Attributes;

namespace Girvs.Application.Services
{
    [DynamicWebApi]
    [AllowAnonymous]
    public class ServiceMethodPermissionService : IServiceMethodPermissionService
    {
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ICacheKeyManager<Permission> _cacheKeyManager;
        private readonly IGirvsAuthorizePermissionManager _girvsAuthorizePermissionManager;

        public ServiceMethodPermissionService(
            IStaticCacheManager staticCacheManager,
            ICacheKeyManager<Permission> cacheKeyManager,
            IGirvsAuthorizePermissionManager girvsAuthorizePermissionManager
        )
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _girvsAuthorizePermissionManager = girvsAuthorizePermissionManager ?? throw new ArgumentNullException(nameof(girvsAuthorizePermissionManager));
        }

        public async Task<List<ServiceMethodPermissionListDto>> Get()
        {
            string key = AppDomain.CurrentDomain.FriendlyName.Replace(".", "_");
            List<ServiceMethodPermissionListDto> list = await _staticCacheManager.GetAsync(
                $"{key}:Permission",
                async () =>
                {
                    var list = await _girvsAuthorizePermissionManager.GetAuthorizePermissionList();
                    return list.Select(a => new ServiceMethodPermissionListDto()
                    {
                        ServiceId = a.ServiceId,
                        ServiceName = a.ServiceName,
                        Permissions = a.Permissions
                    }).ToList();
                },
                _cacheKeyManager.CacheTime);
            return list;
        }
    }
}