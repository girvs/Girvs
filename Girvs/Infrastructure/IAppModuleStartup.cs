namespace Girvs.Infrastructure;

public interface IAppModuleStartup
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    void Configure(IApplicationBuilder application, IWebHostEnvironment env);

    /// <summary>
    /// 端点路由生成器配置
    /// </summary>
    /// <param name="builder">配置器</param>
    void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder);

    int Order { get; }
}
