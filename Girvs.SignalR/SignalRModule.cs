using Girvs.Configuration;
using Girvs.SignalR.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace Girvs.SignalR;

public class SignalRModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var typeFinder = new WebAppTypeFinder();
        // Hub 不应注册为 Singleton：SignalR 的 DefaultHubActivator 会优先从 DI 解析，
        // 单例会让所有连接共享同一个 Hub 实例（状态串用），并可能把 Scoped 依赖提升为 root 生命周期。
        // 注册为 Transient 既保留从容器解析的能力，又保证每次调用使用新实例。
        var hubs = typeFinder.FindOfType<Hub>();
        foreach (var hubType in hubs)
        {
            services.TryAddTransient(hubType);
        }

        hubs = typeFinder.FindOfType(typeof(Hub<>));
        foreach (var hubType in hubs)
        {
            services.TryAddTransient(hubType);
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
