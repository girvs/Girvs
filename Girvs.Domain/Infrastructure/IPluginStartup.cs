using Girvs.Domain.Configuration;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Domain.Infrastructure
{
    /// <summary>
    /// 功能模块插件
    /// </summary>
    public interface IPluginStartup
    {
        /// <summary>
        /// 插件名称
        /// </summary>
        string Name { get;}
        
        /// <summary>
        /// 插件状态
        /// </summary>
        bool Enabled { get;}
        
        /// <summary>
        /// 插件相关的服务注册
        /// </summary>
        /// <param name="services">服务收集器</param>
        /// <param name="typeFinder">类型查找器</param>
        /// <param name="config">相关的配置</param>
        void ConfigureServicesRegister(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config);
        
        /// <summary>
        /// 插件需要使用的中间件配置
        /// </summary>
        /// <param name="application">请求管道生成器</param>
        void ConfigureRequestPipeline(IApplicationBuilder application);

        /// <summary>
        /// 端点路由生成器配置
        /// </summary>
        /// <param name="builder">配置器</param>
        void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder);
        
        /// <summary>
        /// 加载的顺序
        /// </summary>
        int Order { get; }
    }
}