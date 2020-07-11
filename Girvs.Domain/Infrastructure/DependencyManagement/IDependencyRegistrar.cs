using Girvs.Domain.Configuration;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Domain.Infrastructure.DependencyManagement
{
    /// <summary>
    /// 扩展服务依赖注册接口
    /// </summary>
    public interface IDependencyRegistrar
    {
        /// <summary>
        /// 注册服务和接口
        /// </summary>
        /// <param name="services">系统默认服务注册器</param>
        /// <param name="typeFinder">类型查找器</param>
        /// <param name="config">系统配置</param>
        void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config);

        /// <summary>
        /// 获取此依赖关系注册器实现的顺序
        /// </summary>
        int Order { get; }
    }
}