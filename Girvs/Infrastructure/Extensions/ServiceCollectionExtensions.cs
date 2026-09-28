namespace Girvs.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static (IEngine, AppSettings) ConfigureApplicationServices(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
        CommonHelper.DefaultFileProvider = new GirvsFileProvider(webHostEnvironment);

        services.AddHttpContextAccessor();

        var appSettings = new AppSettings();
        services.AddBindAppModelConfiguation(configuration, appSettings);

        var engine = EngineContext.Create();

        engine.ConfigureServices(services, configuration);

        return (engine, appSettings);
    }

    public static TConfig AddConfig<TConfig>(this IServiceCollection services, IConfiguration configuration)
        where TConfig : class, IConfig, new()
    {
        var config = new TConfig();
        configuration.Bind(config.Name, config);
        services.AddSingleton(config);
        return config;
    }


    public static void AddBindAppModelConfiguation(this IServiceCollection services, IConfiguration configuration,
        AppSettings appSettings)
    {
        configuration.Bind(appSettings);
        appSettings.PreLoadModelConfig();
        //判断是否为新环境，如果是环境执行初始化，判断的条件就是AppData下存不存在AppSettings.json文件
        var isInit = !AppSettingsHelper.ExistAppSettingsFile();

        var typeFinder = new WebAppTypeFinder();
        var modelSettings = typeFinder.FindOfType<IAppModuleConfig>();
        var instances = modelSettings
            .Select(startup => (IAppModuleConfig)Activator.CreateInstance(startup));

        foreach (var appModelConfig in instances)
        {
            var nodeName = appModelConfig?.GetType().Name ?? "TempModel";
            configuration.GetSection($"ModuleConfigurations:{nodeName}").Bind(appModelConfig);

            if (isInit)
            {
                appModelConfig?.Init();
            }
            appSettings.ModuleConfigurations.Add(appModelConfig.GetType().Name, appModelConfig);
        }

        services.AddSingleton(appSettings);

        // 配置来自外部分发(GIRVS_SHARED_CONFIG,Aspire AppHost 注入或 K8s ConfigMap 挂载)时不回写:
        // 回写会把共享文件中的动态资源地址固化到本地 appsettings.json,
        // 本地文件优先级更高,下次启动会用固化的旧地址覆盖共享文件的新值。
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GIRVS_SHARED_CONFIG")))
            AppSettingsHelper.SaveAppSettings(appSettings);
        else
            Singleton<AppSettings>.Instance = appSettings;
    }

    public static void AddHttpContextAccessor(this IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    }
}
