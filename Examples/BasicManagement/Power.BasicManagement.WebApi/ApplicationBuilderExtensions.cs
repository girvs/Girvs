using System.Linq;
using Girvs.Domain.GirvsAuthorizePermission;
using Girvs.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Power.BasicManagement.Domain.Repositories;

namespace Power.BasicManagement.WebApi
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseSendAuthorizePermission(this IApplicationBuilder app)
        {
            var service = EngineContext.Current.Resolve<IGirvsAuthorizePermissionManager>();
            var ps = service.GetAuthorizePermissionList().Result;
            var storeService = EngineContext.Current.Resolve<IServicePermissionAuthorizeStore>();


            var list = ps.Select(x => new AuthorizePermissionModel()
            {
                ServiceId = x.ServiceId,
                ServiceName = x.ServiceName,
                Permissions = x.Permissions
            }).ToList();
            storeService.CreateOrUpdate(list);
            return app;
        }
    }
}