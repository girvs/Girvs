using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using AutoMapper;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.Infrastructure.Mapper;
using Girvs.Domain.TypeFinder;
using Girvs.Infrastructure.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Infrastructure.Infrastructure
{
    public class GirvsEngine : IEngine
    {
        private IServiceProvider _serviceProvider { get; set; }
        public virtual IServiceProvider ServiceProvider => _serviceProvider;


        public void ResetServiceProvider(IServiceCollection services)
        {
            _serviceProvider = services.BuildServiceProvider();
        }

        protected IServiceProvider GetServiceProvider()
        {
            var accessor = ServiceProvider.GetService<IHttpContextAccessor>();
            var context = accessor.HttpContext;
            return context?.RequestServices ?? ServiceProvider;
        }

        protected virtual void RunStartupTasks(ITypeFinder typeFinder)
        {
            ////find startup tasks provided by other assemblies
            //var startupTasks = typeFinder.FindClassesOfType<IStartupTask>();

            ////create and sort instances of startup tasks
            ////we startup this interface even for not installed plugins.
            ////otherwise, DbContext initializers won't run and a plugin installation won't work
            //var instances = startupTasks
            //    .Select(startupTask => (IStartupTask)Activator.CreateInstance(startupTask))
            //    .OrderBy(startupTask => startupTask.Order);

            ////execute tasks
            //foreach (var task in instances)
            //    task.Execute();
        }

        protected virtual void RegisterSettings(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig spConfig)
        {
            var settings = typeFinder.FindClassesOfType<ISettings>();
            settings = settings.Where(x => !x.IsInterface);
            foreach (var setting in settings)
                services.AddSingleton(setting);
        }

        protected virtual void RegisterDependencies(IServiceCollection services, ITypeFinder typeFinder,
            GirvsConfig spConfig)
        {
            var dependencyRegistrars = typeFinder.FindClassesOfType<IDependencyRegistrar>();
            var instances = dependencyRegistrars
                .Select(dependencyRegistrar => (IDependencyRegistrar) Activator.CreateInstance(dependencyRegistrar))
                .OrderBy(dependencyRegistrar => dependencyRegistrar.Order);
            foreach (var dependencyRegistrar in instances)
                dependencyRegistrar.Register(services, typeFinder, spConfig);
        }

        protected virtual void AddAutoMapper(IServiceCollection services, ITypeFinder typeFinder)
        {
            var mapperConfigurations = typeFinder.FindClassesOfType<IOrderedMapperProfile>();

            var instances = mapperConfigurations
                .Select(mapperConfiguration => (IOrderedMapperProfile) Activator.CreateInstance(mapperConfiguration))
                .OrderBy(mapperConfiguration => mapperConfiguration.Order);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<DefaultProfile>();
                foreach (var instance in instances)
                {
                    cfg.AddProfile(instance.GetType());
                }
            });

            services.AddSingleton(typeof(IMapper), config.CreateMapper());
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            var tf = Resolve<ITypeFinder>();
            assembly = tf.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            return assembly;
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration, GirvsConfig spConfig)
        {
            var typeFinder = new WebAppTypeFinder();

            services.AddSingleton(typeof(IEngine), this);
            services.AddSingleton(typeof(ITypeFinder), typeFinder);
            ResetServiceProvider(services);
            RegisterSettings(services, typeFinder, spConfig);
            RegisterDependencies(services, typeFinder, spConfig);

            var startupConfigurations = typeFinder.FindClassesOfType<IGirvsStartup>();
            var instances = startupConfigurations
                .Select(startup => (IGirvsStartup) Activator.CreateInstance(startup))
                .OrderBy(startup => startup.Order);
            ResetServiceProvider(services);

            foreach (var instance in instances)
                instance.ConfigureServices(services, configuration);

            ResetServiceProvider(services);
            AddAutoMapper(services, typeFinder);
            RunStartupTasks(typeFinder);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
            var typeFinder = Resolve<ITypeFinder>();
            var startupConfigurations = typeFinder.FindClassesOfType<IGirvsStartup>();

            var instances = startupConfigurations
                .Select(startup => (IGirvsStartup) Activator.CreateInstance(startup))
                .OrderBy(startup => startup.Order);

            foreach (var instance in instances)
                instance.Configure(application);
        }

        public T Resolve<T>() where T : class
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            return GetServiceProvider().GetService(type);
        }

        public virtual IEnumerable<T> ResolveAll<T>()
        {
            return (IEnumerable<T>) GetServiceProvider().GetServices(typeof(T));
        }

        public virtual object ResolveUnregistered(Type type)
        {
            Exception innerException = null;
            foreach (var constructor in type.GetConstructors())
            {
                try
                {
                    var parameters = constructor.GetParameters().Select(parameter =>
                    {
                        var service = Resolve(parameter.ParameterType);
                        if (service == null)
                            throw new GirvsException("未知依赖");
                        return service;
                    });

                    return Activator.CreateInstance(type, parameters.ToArray());
                }
                catch (Exception ex)
                {
                    innerException = ex;
                }
            }

            throw new GirvsException("没有找到满足所有依赖关系的构造函数。", innerException);
        }

        public HttpContext HttpContext
        {
            get
            {
                var accessor = ServiceProvider.GetService<IHttpContextAccessor>();
                return accessor.HttpContext;
            }
        }

        public Guid CurrentClaimSid
        {
            get
            {
                var disclaims = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Sid);
                return Guid.Parse(disclaims?.Value);
            }
        }

        public Guid CurrentClaimTenantId
        {
            get
            {
                var disclaims = HttpContext.User.Claims.FirstOrDefault(x => x.Type == "TenantId");
                return Guid.Parse(disclaims?.Value);
            }
        }
    }
}