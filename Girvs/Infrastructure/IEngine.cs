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

    TConfig GetAppModuleConfig<TConfig>()
        where TConfig : class, IAppModuleConfig;

    void SetCurrentThreadServiceProvider(IServiceProvider serviceProvider);
}
