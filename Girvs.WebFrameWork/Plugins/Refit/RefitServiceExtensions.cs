using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Girvs.WebFrameWork.Plugins.Refit
{
    public static class RefitServiceExtensions
    {
        public static IHttpClientBuilder AddGirvsRefitClient(this IServiceCollection services, Type t,
            RefitSettings settings = null)
        {
            Type serviceType = typeof(RefitServiceExtensions);
            MethodInfo mi = serviceType.GetMethod(nameof(RefitServiceExtensions.AddObjectRefitClient));
            if (mi != null)
            {
                MethodInfo dmi = mi.MakeGenericMethod(t);
                return dmi.Invoke(serviceType, new object[] { services, settings }) as IHttpClientBuilder;
            }

            return null;
        }

        public static IHttpClientBuilder AddObjectRefitClient<TContext>(this IServiceCollection services, RefitSettings settings = null) where TContext : class
        {
            return services.AddRefitClient<TContext>(settings);
        }
    }
}