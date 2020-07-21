using System;
using System.Linq;
using Girvs.Application;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.FileProvider;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.IRepositories;
using Girvs.Domain.Managers;
using Girvs.Domain.TypeFinder;
using Girvs.Infrastructure.FileProvider;
using Girvs.Infrastructure.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Girvs.WebFrameWork.Infrastructure.ServicesExtensions
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureApplicationServices(this IServiceCollection services,
            IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            var config = services.ConfigureStartupConfig<GirvsConfig>(configuration.GetSection("Girvs"));
            services.ConfigureStartupConfig<HostingConfig>(configuration.GetSection("Hosting"));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            CommonHelper.DefaultFileProvider = new GirvsFileProvider(hostingEnvironment);
            services.AddSingleton<IGirvsFileProvider>(CommonHelper.DefaultFileProvider);
            services.AddSingleton<IFileProvider>(CommonHelper.DefaultFileProvider);
            var engine = EngineContext.Replace(new GirvsEngine());
            engine.ConfigureServices(services, configuration, config);
            engine.ResetServiceProvider(services);
            engine.Resolve<ILogger<object>>().LogInformation("应用程序开始启动");
        }

        /// <summary>
        /// 创建，绑定并将指定配置参数注册为服务
        /// </summary>
        /// <typeparam name="TConfig">配置类型</typeparam>
        /// <param name="services">依懒服务注册提供者</param>
        /// <param name="configuration">系统应用配置</param>
        /// <returns>配置实例</returns>
        public static TConfig ConfigureStartupConfig<TConfig>(this IServiceCollection services,
            IConfiguration configuration) where TConfig : class, new()
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            //创建配置实例
            var config = new TConfig();

            //将其绑定到配置的适当部分
            configuration.Bind(config);

            //并将其注册为服务
            services.AddSingleton(config);

            return config;
        }


        /// <summary>
        /// 添加和配置默认的HTTP客户端
        /// </summary>
        public static void AddSpHttpClients(this IServiceCollection services)
        {
            //default client
            //services.AddHttpClient(SpHttpDefaults.DefaultHttpClient).WithProxy();

            ////client to request current store
            //services.AddHttpClient<StoreHttpClient>();

            ////client to request spCommerce official site
            //services.AddHttpClient<SpHttpClient>().WithProxy();

            ////client to request reCAPTCHA service
            //services.AddHttpClient<CaptchaHttpClient>().WithProxy();
        }


        /// <summary>
        /// 注册业务相关的服务至Services
        /// </summary>
        /// <param name="services"></param>
        /// <param name="typeFinder"></param>
        public static void AddRegisterBusinessServices(this IServiceCollection services, ITypeFinder typeFinder)
        {
            services.RegisterType(typeof(IRepository<>),  typeFinder);
            services.RegisterType<IManager>(typeFinder);
            services.RegisterType<IService>(typeFinder);
        }

        public static void RegisterType<T>(this IServiceCollection services, ITypeFinder typeFinder)
        {
            services.RegisterType(typeof(T), typeFinder);
        }

        public static void RegisterType(this IServiceCollection services, Type type, ITypeFinder typeFinder)
        {
            var types = typeFinder.FindClassesOfType(type, false, true);
            var interFaceTypes = types.Where(x => x.IsInterface && x.Name != type.Name).ToList();
            foreach (var repositoryType in interFaceTypes)
            {
                var targetType = types.FirstOrDefault(x => repositoryType.IsAssignableFrom(x) && x.IsClass);
                if (targetType != null)
                {
                    services.AddScoped(repositoryType, targetType);
                }
            }
        }
    }
}