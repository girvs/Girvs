using System;
using System.Linq;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.FileProvider;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Girvs.Infrastructure.FileProvider;
using Girvs.Infrastructure.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Girvs.Service.FrameWork.Plugins
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

        public static void RegisterType<T>(this IServiceCollection services, ITypeFinder typeFinder, bool onlyInterface = true)
        {
            services.RegisterType(typeof(T), typeFinder, onlyInterface);
        }

        public static void RegisterType(this IServiceCollection services, Type type, ITypeFinder typeFinder, bool onlyInterface = true)
        {
            var types = typeFinder.FindClassesOfType(type, false, true);
            var interFaceTypes = types.Where(x => (!onlyInterface || x.IsInterface) && x.Name != type.Name).ToList();
            foreach (var repositoryType in interFaceTypes)
            {
                var targetType = typeFinder.FindClassesOfType(repositoryType).ToList().FirstOrDefault(x => x.IsClass);
                if (targetType != null)
                {
                    services.AddScoped(repositoryType, targetType);
                }
            }
        }
        
        public static void RegisterType(this IServiceCollection services, Type type, ITypeFinder typeFinder ,Type asType = null)
        {
            var types = typeFinder.FindClassesOfType(type, false, true);
            var interFaceTypes = types.Where(x => x.Name != type.Name).ToList();
            foreach (var repositoryType in interFaceTypes)
            {
                var implementedInterfaces = ((System.Reflection.TypeInfo) repositoryType).ImplementedInterfaces.ToList();
                if (implementedInterfaces.Any())
                {
                    foreach (var bcType in implementedInterfaces)
                    {
                        if (asType != null)
                        {
                            services.AddScoped(asType, bcType);
                        }
                        else
                        {
                            services.AddScoped(bcType, repositoryType);
                        }
                    }
                }
            }
        }
        
        
        public static void RegisterIValidatorType(this IServiceCollection services, Type type, ITypeFinder typeFinder)
        {
            var types = typeFinder.FindClassesOfType(type, true, false);
            foreach (var validatorType in types)
            {
                var parentType =
                    ((System.Reflection.TypeInfo) validatorType).ImplementedInterfaces.FirstOrDefault(x =>
                        x.Name == "IValidator`1");
                if (parentType != null)
                {
                    services.AddScoped(parentType, validatorType);
                }
            }
        }

    }
}