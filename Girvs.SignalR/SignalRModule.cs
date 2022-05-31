using Girvs.Cache.Configuration;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Girvs.SignalR
{
    public class SignalRModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var typeFinder = new WebAppTypeFinder();
            var hubs = typeFinder.FindOfType<Hub>();
            foreach (var hubType in hubs)
            {
                services.AddSingleton(hubType);
            }

            hubs = typeFinder.FindOfType(typeof(Hub<>));
            foreach (var hubType in hubs)
            {
                services.AddSingleton(hubType);
            }

            var signalServiceBuilder = services.AddSignalR().AddMessagePackProtocol();
            var cacheConfig = EngineContext.Current.GetAppModuleConfig<CacheConfig>();
            if (cacheConfig.EnableCaching && cacheConfig.DistributedCacheType == CacheType.Redis)
            {
                signalServiceBuilder.AddRedis(cacheConfig.RedisCacheConfig.ConnectionString);
            }

            services.TryAddEnumerable(ServiceDescriptor
                .Singleton<IPostConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>());
        }

        public void Configure(IApplicationBuilder application)
        {
            // application.Use((context, next) =>
            // {
            //     if (context.Request.Query.TryGetValue("access_token",out var token))
            //     {
            //         context.Request.Headers.Add("Authorization",$"Bearer {token}");
            //     }
            //
            //     return next.Invoke();
            // });
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            builder.AutoMapSignalREndpointRouteBuilder();
        }

        public int Order { get; } = 99903;
    }
}