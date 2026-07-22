namespace Girvs.Infrastructure;

public interface IEngine
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    void ConfigureRequestPipeline(IApplicationBuilder application, IWebHostEnvironment env);

    void ConfigureEndpointRouteBuilder(IEndpointRouteBuilder endpointRouteBuilder);

    T Resolve<T>(IServiceScope scope = null)
        where T : class;

    object Resolve(Type type, IServiceScope scope = null);

    IEnumerable<T> ResolveAll<T>();

    object ResolveUnregistered(Type type);

    /// <summary>
    /// 相关的上下文信息
    /// </summary>
    HttpContext HttpContext { get; }

    bool IsAuthenticated { get; }

    IGirvsClaimManager ClaimManager { get; }

    IGirvsPrincipalAccessor PrincipalAccessor { get; }

    TConfig GetAppModuleConfig<TConfig>()
        where TConfig : class, IAppModuleConfig;

    void SetCurrentThreadServiceProvider(IServiceProvider serviceProvider);

    /// <summary>
    /// 在当前异步执行流内临时切换环境服务提供程序（作用域）。
    /// 返回的令牌被释放（Dispose）时，会自动还原为切换前的值，而非粗暴置空，从而支持嵌套并避免污染其它并发上下文。
    /// 适用于后台任务、消息消费等无 HttpContext 的场景。
    /// </summary>
    /// <param name="serviceProvider">要切换到的服务提供程序（通常是某个独立作用域的 ServiceProvider）</param>
    /// <returns>还原令牌，务必配合 using 使用</returns>
    IDisposable ChangeCurrentThreadServiceProvider(IServiceProvider serviceProvider);
}
