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
        services.AddBindSerilogConfiguation(configuration);

        var engine = EngineContext.Create();

        engine.ConfigureServices(services, configuration);

        // ServiceContextFactory.Create(services.BuildServiceProvider());
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


    public static void AddBindSerilogConfiguation(this IServiceCollection services, IConfiguration configuration)
    {
        if (!AppSettingsHelper.ExistSerilogConfigFile())
        {
            AppSettingsHelper.CreateSerilogConfig(SerilogInitConfig.GetJsonString());
        }
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
        AppSettingsHelper.SaveAppSettings(appSettings);
    }

    public static void AddHttpContextAccessor(this IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    }
}