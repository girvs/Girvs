namespace Girvs.Infrastructure;

/// <summary>
/// Represents Nop engine
/// </summary>
public class GirvsEngine : IEngine
{
    /// <summary>
    /// Get I
    /// </summary>
    /// <returns>IServiceProvider</returns>
    // 访问器来自根容器且为单例，缓存一次即可；容器变更时由 ServiceProvider 的 setter 清空。
    private IHttpContextAccessor _httpContextAccessor;

    private IHttpContextAccessor HttpContextAccessor =>
        _httpContextAccessor ??= ServiceProvider?.GetService<IHttpContextAccessor>();

    protected IServiceProvider GetServiceProvider(IServiceScope scope = null)
    {
        if (scope == null)
        {
            // 原实现每次解析服务都走一次 GetService<IHttpContextAccessor>()，现已改为缓存读取。
            var context = HttpContextAccessor?.HttpContext;
            return context?.RequestServices ?? AsyncLocalServiceProvider.Value ?? ServiceProvider;
        }

        return scope.ServiceProvider;
    }

    private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        //check for assembly already loaded
        var assembly = AppDomain
            .CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.FullName == args.Name);
        if (assembly != null)
            return assembly;

        //get assembly from TypeFinder
        var tf = Resolve<ITypeFinder>();
        assembly = tf?.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
        return assembly;
    }

    /// <summary>
    /// Add and configure services
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        //find startup configurations provided by other assemblies
        var typeFinder = new WebAppTypeFinder();
        //register engine
        services.AddSingleton<IEngine>(this);

        //register type finder
        services.AddSingleton<ITypeFinder>(typeFinder);

        var startupConfigurations = typeFinder.FindOfType<IAppModuleStartup>();

        //create and sort instances of startup configurations
        var instances = startupConfigurations
            .Select(startup => (IAppModuleStartup)Activator.CreateInstance(startup))
            .OrderBy(startup => startup?.Order);

        //configure services
        foreach (var instance in instances)
            instance?.ConfigureServices(services, configuration);

        //resolve assemblies here. otherwise, plugins can throw an exception when rendering views
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    /// <summary>
    /// Configure HTTP request pipeline
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    public void ConfigureRequestPipeline(IApplicationBuilder application, IWebHostEnvironment env)
    {
        ServiceProvider = application.ApplicationServices;

        //find startup configurations provided by other assemblies
        var typeFinder = Resolve<ITypeFinder>();
        var startupConfigurations = typeFinder.FindOfType<IAppModuleStartup>();

        //create and sort instances of startup configurations
        var instances = startupConfigurations
            .Select(startup => (IAppModuleStartup)Activator.CreateInstance(startup))
            .OrderBy(startup => startup?.Order);

        //configure request pipeline
        foreach (var instance in instances)
            instance?.Configure(application, env);
    }

    /// <summary>
    /// Configure pipeline endpoint
    /// </summary>
    /// <param name="endpointRouteBuilder"></param>
    public void ConfigureEndpointRouteBuilder(IEndpointRouteBuilder endpointRouteBuilder)
    {
        var typeFinder = Resolve<ITypeFinder>();
        var startupConfigurations = typeFinder.FindOfType<IAppModuleStartup>();

        var instances = startupConfigurations
            .Select(startup => (IAppModuleStartup)Activator.CreateInstance(startup))
            .OrderBy(startup => startup.Order);

        var logger = Resolve<ILogger<object>>();
        foreach (var instance in instances)
        {
            // 结构化模板：日志级别关闭时不再产生插值字符串。
            logger.LogInformation("开始配置 {StartupName} 相关的映射节点 EndpointRouteBuilder", instance.GetType().Name);
            instance.ConfigureMapEndpointRoute(endpointRouteBuilder);
        }
    }

    /// <summary>
    /// Resolve dependency
    /// </summary>
    /// <param name="scope">Scope</param>
    /// <typeparam name="T">Type of resolved service</typeparam>
    /// <returns>Resolved service</returns>
    public T Resolve<T>(IServiceScope scope = null)
        where T : class
    {
        return (T)Resolve(typeof(T), scope);
    }

    /// <summary>
    /// Resolve dependency
    /// </summary>
    /// <param name="type">Type of resolved service</param>
    /// <param name="scope">Scope</param>
    /// <returns>Resolved service</returns>
    public object Resolve(Type type, IServiceScope scope = null)
    {
        return GetServiceProvider(scope)?.GetService(type);
    }

    /// <summary>
    /// Resolve dependencies
    /// </summary>
    /// <typeparam name="T">Type of resolved services</typeparam>
    /// <returns>Collection of resolved services</returns>
    public virtual IEnumerable<T> ResolveAll<T>()
    {
        return (IEnumerable<T>)GetServiceProvider().GetServices(typeof(T));
    }

    /// <summary>
    /// Resolve unregistered service
    /// </summary>
    /// <param name="type">Type of service</param>
    /// <returns>Resolved service</returns>
    public virtual object ResolveUnregistered(Type type)
    {
        Exception innerException = null;
        foreach (var constructor in type.GetConstructors())
        {
            try
            {
                //try to resolve constructor parameters
                var parameters = constructor
                    .GetParameters()
                    .Select(parameter =>
                    {
                        var service = Resolve(parameter.ParameterType);
                        if (service == null)
                            throw new GirvsException("Unknown dependency");
                        return service;
                    });

                //all is ok, so create instance
                return Activator.CreateInstance(type, parameters.ToArray());
            }
            catch (Exception ex)
            {
                innerException = ex;
            }
        }

        throw new GirvsException(
            "No constructor was found that had all the dependencies satisfied.",
            innerException
        );
    }

    /// <summary>
    /// Register dependencies
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="appSettings">App settings</param>
    public virtual void RegisterDependencies(IServiceCollection services, AppSettings appSettings)
    {
        var typeFinder = new WebAppTypeFinder();

        //register engine
        services.AddSingleton<IEngine>(this);

        //register type finder
        services.AddSingleton<ITypeFinder>(typeFinder);

        //find dependency registrars provided by other assemblies
        var dependencyRegistrars = typeFinder.FindOfType<IDependencyRegistrar>();

        //create and sort instances of dependency registrars
        var instances = dependencyRegistrars
            .Select(dependencyRegistrar =>
                (IDependencyRegistrar)Activator.CreateInstance(dependencyRegistrar)
            )
            .OrderBy(dependencyRegistrar => dependencyRegistrar?.Order);

        //register all provided dependencies
        foreach (var dependencyRegistrar in instances)
            dependencyRegistrar?.Register(services, typeFinder, appSettings);

        services.AddSingleton(services);
    }

    public HttpContext HttpContext => HttpContextAccessor?.HttpContext;

    public IGirvsPrincipalAccessor PrincipalAccessor => Resolve<IGirvsPrincipalAccessor>();

    public TConfig GetAppModuleConfig<TConfig>()
        where TConfig : class, IAppModuleConfig
    {
        return Singleton<AppSettings>.Instance.Get<TConfig>();
    }

    public void SetCurrentThreadServiceProvider(IServiceProvider serviceProvider)
    {
        AsyncLocalServiceProvider.Value = serviceProvider;
    }

    public IDisposable ChangeCurrentThreadServiceProvider(IServiceProvider serviceProvider)
    {
        var previous = AsyncLocalServiceProvider.Value;
        AsyncLocalServiceProvider.Value = serviceProvider;
        return new ServiceProviderScopeToken(previous);
    }

    /// <summary>
    /// 作用域还原令牌：释放时把环境服务提供程序还原为切换前的值。
    /// </summary>
    private sealed class ServiceProviderScopeToken(IServiceProvider previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            AsyncLocalServiceProvider.Value = previous;
        }
    }

    public bool IsAuthenticated =>
        PrincipalAccessor.Principal.Identity?.IsAuthenticated == true;

    private static readonly AsyncLocal<IServiceProvider> AsyncLocalServiceProvider =
        new AsyncLocal<IServiceProvider>();

    private IServiceProvider _serviceProvider;

    public virtual IServiceProvider ServiceProvider
    {
        get => _serviceProvider;
        protected set
        {
            _serviceProvider = value;
            // 容器替换后，从新容器重新解析访问器。
            _httpContextAccessor = null;
        }
    }

    /// <summary>
    /// 由主机构建完成后回填实际服务提供程序，避免在注册阶段创建第二套容器。
    /// </summary>
    internal void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}
