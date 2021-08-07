using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Girvs.DynamicWebApi;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Authorization;
using Panda.DynamicWebApi.Attributes;

namespace Girvs.AuthorizePermission.ActionPermission
{
    [DynamicWebApi]
    [AllowAnonymous]
    public class GirvsAuthorizePermissionService : IGirvsAuthorizePermissionService
    {
        public Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList()
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var services = typeFinder.FindOfType<IAppWebApiService>()
                .Where(x => x.IsDefined(typeof(ServicePermissionDescriptorAttribute), false));

            var list = services.Select(service =>
            {
                var spd =
                    service.GetCustomAttribute(typeof(ServicePermissionDescriptorAttribute)) as
                        ServicePermissionDescriptorAttribute;

                var methodInfos = service.GetMethods().Where(x =>
                    x.IsPublic && x.IsDefined(typeof(ServiceMethodPermissionDescriptorAttribute), false));


                var permissions = new Dictionary<string, string>();
                foreach (var methodInfo in methodInfos)
                {
                    var smpd =
                        methodInfo.GetCustomAttribute(typeof(ServiceMethodPermissionDescriptorAttribute)) as
                            ServiceMethodPermissionDescriptorAttribute;

                    var permissionStr = smpd.Permission.ToString();
                    if (!permissions.ContainsValue(permissionStr) && !permissions.ContainsKey(smpd.MethodName))
                    {
                        permissions.Add(smpd.MethodName, smpd.Permission.ToString());
                    }
                }

                return new AuthorizePermissionModel
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