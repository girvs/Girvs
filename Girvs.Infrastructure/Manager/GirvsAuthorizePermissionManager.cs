using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Girvs.Domain.GirvsAuthorizePermission;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Girvs.Domain.TypeFinder;

namespace Girvs.Infrastructure.Manager
{
    public class GirvsAuthorizePermissionManager : IGirvsAuthorizePermissionManager
    {
        public Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList()
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var services = typeFinder.FindClassesOfType<IManager>(onlyConcreteClasses: true, includeInterFace: false)
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