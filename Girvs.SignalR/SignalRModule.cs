using Girvs.Configuration;
using Girvs.SignalR.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace Girvs.SignalR;

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
        var signalRConfig = EngineContext.Current.GetAppModuleConfig<SignalRConfig>();
        if (!string.IsNullOrWhiteSpace(signalRConfig.RedisConnectionRef))
        {
            var resource = Singleton<AppSettings>.Instance.Resources.TryGetValue(
                signalRConfig.RedisConnectionRef,
                out var value
            )
                ? value
                : throw new GirvsException($"Resources:{signalRConfig.RedisConnectionRef} 未配置");

            if (!string.Equals(resource.Type, "redis", StringComparison.OrdinalIgnoreCase))
                throw new GirvsException($"Resources:{signalRConfig.RedisConnectionRef}:Type 必须为 redis");

            var connectionString = resource.Settings.GetValueOrDefault("Endpoints")
                ?? throw new GirvsException($"Resources:{signalRConfig.RedisConnectionRef}:Settings:Endpoints 未配置");

            signalServiceBuilder.AddStackExchangeRedis(connectionString, options =>
            {
                options.Configuration.ChannelPrefix = "Message";
                options.Configuration.ConnectTimeout = 5000;
                options.Configuration.KeepAlive = 60;
            });
        }

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IPostConfigureOptions<JwtBearerOptions>,
                ConfigureJwtBearerOptions
            >()
        );
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
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
