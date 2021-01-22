using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Girvs.Application.Attributes;
using Girvs.Application.Dtos;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Enumerations;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Girvs.Domain.TypeFinder;
using Panda.DynamicWebApi.Attributes;

namespace Girvs.Application.Services
{
    [DynamicWebApi]
    public class ServiceMethodPermissionService : IServiceMethodPermissionService
    {
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ICacheKeyManager<Permission> _cacheKeyManager;

        public ServiceMethodPermissionService(
            IStaticCacheManager staticCacheManager,
            ICacheKeyManager<Permission> cacheKeyManager
        )
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
        }

        public async Task<List<ServiceMethodPermissionListDto>> Get()
        {
            List<ServiceMethodPermissionListDto> list = await _staticCacheManager.GetAsync(
                _cacheKeyManager.BuildCacheEntityKey(nameof(Permission)),
                async () => await BuilderPermissionListDtos(),
                _cacheKeyManager.CacheTime);
            return list;
        }

        private Task<List<ServiceMethodPermissionListDto>> BuilderPermissionListDtos()
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var services = typeFinder.FindClassesOfType<IManager>(onlyConcreteClasses:true,includeInterFace:false)
                .Where(x => x.IsDefined(typeof(ServicePermissionDescriptorAttribute), false));

            var list = services.Select(service =>
            {
                var spd =
                    service.GetCustomAttribute(typeof(ServicePermissionDescriptorAttribute)) as
                        ServicePermissionDescriptorAttribute;

                var methodInfos = service.GetMethods().Where(x =>
                    x.IsPublic && x.IsDefined(typeof(ServiceMethodPermissionDescriptorAttribute), false));


                Dictionary<string, string> permissions = new Dictionary<string, string>();
                foreach (var methodInfo in methodInfos)
                {
                    var smpd =
                        methodInfo.GetCustomAttribute(typeof(ServiceMethodPermissionDescriptorAttribute)) as
                            ServiceMethodPermissionDescriptorAttribute;
                    permissions.Add(smpd.MethodName, smpd.Permission.ToString());
                }

                return new ServiceMethodPermissionListDto
                {
                    ServiceName = spd.ServiceName,
                    ServiceId = spd.ServiceId,
                    Permissions = permissions
                };
            }).ToList();

            return Task.FromResult(list);
        }
    }
}