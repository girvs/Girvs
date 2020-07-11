using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Domain.Infrastructure
{
    /// <summary>
    /// 代表用于在应用程序启动时配置服务和中间件的对象
    /// </summary>
    public interface IGirvsStartup
    {
        /// <summary>
        /// 注册相关中间件需要的服务
        /// </summary>
        /// <param name="services">服务依赖提供者</param>
        /// <param name="configuration">系统应用程序配置</param>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// 配置使用添加的中间件
        /// </summary>
        /// <param name="application">请求管道生成器</param>
        void Configure(IApplicationBuilder application);

        /// <summary>
        /// 获取此启动配置实现的顺序
        /// </summary>
        int Order { get; }
    }
}