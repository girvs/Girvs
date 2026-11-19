using DotNetCore.CAP.Internal;
using Girvs.Configuration.Resources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Girvs.EventBus;

/// <summary>
/// 基于 CAP 的集成事件发布/订阅模块。
/// <para>
/// CAP 消费链路时序（官方源码 v8.3.1/v10.0.2 一致）：每条消息由 InvokeAsync 创建专属作用域
/// （CreateAsyncScope）→ GetInstance 从该作用域构造 handler → filter.OnSubscribeExecutingAsync
/// → handler.Handle → filter.OnSubscribeExecutedAsync（异常走 OnSubscribeExceptionAsync）
/// → 方法退出时释放作用域。
/// </para>
/// <para>
/// EngineContext 桥接分工：GirvsSubscribeInvoker 负责 handler 构造期的桥接；
/// GirvsCapFilter 负责贯穿 Handle 全程的桥接与身份恢复（其构造注入的 IServiceProvider
/// 即本消息作用域的 provider）。因此 handler 的构造注入与 Handle 内经 EngineContext.Resolve
/// 取得的服务落在同一消息作用域（同一 DbContext 实例），满足多 Pod / 多线程并发下每消息隔离。
/// </para>
/// </summary>
public class EventBusModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        //Note: The injection of services needs before of `services.AddCap()`
        services.AddCapSubscribe();

        var eventBusConfig = EngineContext.Current.GetAppModuleConfig<EventBusConfig>();

        services.AddScoped<IEventBus, CapEventBus.CapEventBus>();

        // 资源引用为数组：索引 0 为资源键格式串，索引 1 起同时用于组合资源键与格式化 Settings 模板
        var persistenceResource = InfrastructureResourceReferenceResolver.GetRequiredResource(
            eventBusConfig.PersistenceConnectionRef
        );
        var transportResource = InfrastructureResourceReferenceResolver.GetRequiredResource(
            eventBusConfig.TransportConnectionRef
        );

        var connStr = InfrastructureResourceReferenceResolver.GetRequiredSetting(
            eventBusConfig.PersistenceConnectionRef,
            "ConnectionString"
        );
        var persistenceType = (persistenceResource.Type ?? string.Empty).ToLowerInvariant();
        var transportType = (transportResource.Type ?? string.Empty).ToLowerInvariant();

        // 必须先于 AddCap 注册（CAP 内部对 ISubscribeInvoker 用 TryAddSingleton，先到先得）：
        // 作用是让 handler 构造期的 EngineContext.Resolve 也落在本消息的专属作用域
        services.AddSingleton<ISubscribeInvoker, GirvsSubscribeInvoker>();

        // 注册期即校验配置白名单，消除 Validate* 与 switch 双清单脱节时的静默假阳性：
        // 一旦脱节，启动即抛 GirvsException 暴露配置问题
        ValidatePersistenceTypeSupported(persistenceType);
        ValidateTransportTypeSupported(transportType);

        services
            .AddCap(x =>
            {
                switch (persistenceType)
                {
                    case "sqlserver":
                        x.UseSqlServer(connStr);
                        break;
                    case "mysql":
                        x.UseMySql(connStr);
                        break;
                    case "sqlite":
                        x.UseSqlite(connStr);
                        break;
                    default:
                        // fail-fast：未知持久化类型立即失败，错误信息携带实际配置类型
                        throw NotSupportedPersistenceType(persistenceType);
                }

                switch (transportType)
                {
                    case "rabbitmq":
                        x.UseRabbitMQ(options =>
                        {
                            options.ConnectionFactoryOptions = factory =>
                            {
                                factory.Uri = new Uri(
                                    InfrastructureResourceReferenceResolver.GetRequiredSetting(
                                        eventBusConfig.TransportConnectionRef,
                                        "ConnectionString"
                                    )
                                );
                            };
                        });
                        break;
                    case "kafka":
                        x.UseKafka(configure =>
                        {
                            var raw =
                                InfrastructureResourceReferenceResolver.GetRequiredSetting(
                                    eventBusConfig.TransportConnectionRef,
                                    "ConnectionString"
                                );

                            var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);

                            // 第一段是 bootstrap servers
                            configure.Servers = parts[0];

                            // 后续段是 MainConfig 键值对
                            foreach (var part in parts.Skip(1))
                            {
                                var kv = part.Split('=', 2);
                                if (kv.Length == 2)
                                {
                                    configure.MainConfig[kv[0].Trim()] = kv[1].Trim();
                                }
                            }
                        });
                        break;
                    case "redis":
                        x.UseRedis(
                            InfrastructureResourceReferenceResolver.GetRequiredSetting(
                                eventBusConfig.TransportConnectionRef,
                                "Endpoints"
                            )
                        );
                        break;
                    default:
                        // fail-fast：未知传输类型立即失败，错误信息携带实际配置类型
                        throw NotSupportedTransportType(transportType);
                }

                if (eventBusConfig.EnableDashboard)
                {
                    // Dashboard 无鉴权，默认关闭；仅显式开启时才挂载路由
                    x.UseDashboard(d =>
                    {
                        var hostEnv = EngineContext.Current.Resolve<IHostEnvironment>();
                        // 解析不到环境（非 Web 宿主）时保守设置 PathBase，维持生产语义
                        if (hostEnv == null || !hostEnv.IsDevelopment())
                            d.PathBase = $"/{ServiceNameResolver.FromAssemblyName()}";
                    });
                }

                x.ConsumerThreadCount = eventBusConfig.ConsumerThreadCount;
                x.SucceedMessageExpiredAfter = eventBusConfig.SucceedMessageExpiredAfter;
                x.FailedMessageExpiredAfter = eventBusConfig.FailedMessageExpiredAfter;
                // x.ProducerThreadCount = eventBusConfig.ProducerThreadCount;
            })
            .AddSubscribeFilter<GirvsCapFilter>();

        // fail-fast 校验：AddSubscribeFilter 内部是 TryAddScoped，若宿主在 AddCap 之前已注册过
        // 其他 ISubscribeFilter，GirvsCapFilter 会被静默放弃，EventBus 的身份恢复与作用域桥接将失效
        var subscribeFilterDescriptor = services.FirstOrDefault(
            x => x.ServiceType == typeof(ISubscribeFilter));
        if (subscribeFilterDescriptor?.ImplementationType != typeof(GirvsCapFilter))
        {
            throw new GirvsException(
                "已存在其他 ISubscribeFilter 注册，GirvsCapFilter 无法生效，EventBus 的身份恢复与作用域桥接将失效");
        }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    // 未知持久化/传输类型的 fail-fast 错误，供 switch default 分支与内部校验共用
    internal static GirvsException NotSupportedPersistenceType(string persistenceType) =>
        new($"不支持的 EventBus 持久化类型: {persistenceType}");

    internal static GirvsException NotSupportedTransportType(string transportType) =>
        new($"不支持的 EventBus 传输类型: {transportType}");

    // switch 的类型判断提取为静态辅助：供注册期 fail-fast 校验，并可直接单测；
    // switch default 分支保留同源 fail-fast，双保险
    internal static void ValidatePersistenceTypeSupported(string persistenceType)
    {
        if (persistenceType is not ("sqlserver" or "mysql" or "sqlite"))
        {
            throw NotSupportedPersistenceType(persistenceType);
        }
    }

    internal static void ValidateTransportTypeSupported(string transportType)
    {
        if (transportType is not ("rabbitmq" or "kafka" or "redis"))
        {
            throw NotSupportedTransportType(transportType);
        }
    }

    public int Order { get; } = 50000;
}
