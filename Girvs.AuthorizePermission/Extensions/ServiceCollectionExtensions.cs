using System;
using Girvs.AuthorizePermission.AuthorizeCompare;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.AuthorizePermission.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IMvcBuilder AddControllersWithAuthorizePermissionFilter(this IServiceCollection services)
        {
            return services.AddControllers(option =>
            {
                option.Filters.Add<ActionPermissionFilter>();
            });
        }
        
        
        public static IMvcBuilder AddControllersWithAuthorizePermissionFilter(this IServiceCollection services,Action<MvcOptions> configure)
        {
            return services.AddControllers(option =>
            {
                option.Filters.Add<ActionPermissionFilter>();
                configure(option);
            });
        }
    }
}