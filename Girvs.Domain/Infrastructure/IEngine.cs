using System;
using System.Collections.Generic;
using Girvs.Domain.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Domain.Infrastructure
{
    /// <summary>
    /// 实现此接口的类可以用作组成Sp引擎的各种服务的门户。
    ///编辑功能，模块和实现通过此界面访问大多数Sp功能。
    /// </summary>
    public interface IEngine
    {
        /// <summary>
        /// 添加和配置服务
        /// </summary>
        /// <param name="services">服务描述符的集合</param>
        /// <param name="configuration">应用程序配置</param>
        /// <param name="spConfig">Sp配置参数</param>
        /// <returns>服务提供者</returns>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration, GirvsConfig spConfig);

        void ResetServiceProvider(IServiceCollection services);
        /// <summary>
        /// 配置HTTP请求管道
        /// </summary>
        /// <param name="application">用于配置应用程序的请求管道的生成器</param>
        void ConfigureRequestPipeline(IApplicationBuilder application);

        /// <summary>
        /// 获取注册类型的实例
        /// </summary>
        /// <typeparam name="T">解决的服务类型</typeparam>
        /// <returns>获取到的服务</returns>
        T Resolve<T>() where T : class;

        /// <summary>
        /// 获取注册类型的实例
        /// </summary>
        /// <param name="type">服务类型</param>
        /// <returns>获取到的服务</returns>
        object Resolve(Type type);

        /// <summary>
        /// 获取注册类型的实例集合
        /// </summary>
        /// <typeparam name="T">基类类型</typeparam>
        /// <returns>收集已解决的服务</returns>
        IEnumerable<T> ResolveAll<T>();

        /// <summary>
        /// 通过反射获取未注册的服务
        /// </summary>
        /// <param name="type">服务类型</param>
        /// <returns>服务实例</returns>
        object ResolveUnregistered(Type type);

        /// <summary>
        /// 相关的上下文信息
        /// </summary>
        HttpContext HttpContext { get; }

        Guid CurrentClaimSid { get; }
        Guid CurrentClaimTenantId { get; }
    }
}