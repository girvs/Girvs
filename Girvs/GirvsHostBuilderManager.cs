using Girvs.Infrastructure.Extensions;
using Serilog;
using Serilog.Events;

namespace Girvs;

public static class GirvsHostBuilderManager
{
    /// <summary>
    /// 旧拼写兼容方法，转发到 CreateGirvsHostBuilder
    /// </summary>
    [Obsolete("拼写修正：请使用 CreateGirvsHostBuilder<TStartup>(args)")]
    public static IHostBuilder CreateGrivsHostBuilder<TStartup>(string[] args)
        where TStartup : class, IGirvsStartup => CreateGirvsHostBuilder<TStartup>(args);

    public static IHostBuilder CreateGirvsHostBuilder<TStartup>(string[] args)
        where TStartup : class, IGirvsStartup
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.HostUseSerilog();
        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder
                .ConfigureAppConfiguration((hostingContext, appConfigBuilder) =>
                    //清除源有的源
                    appConfigBuilder.HostUseGirvsConfig(hostingContext.HostingEnvironment, args)
                )
                .UseStartup<TStartup>();
        });

        return builder;
    }

    public static void HostUseSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception}{NewLine}"
                )
                .ReadFrom.Configuration(context.Configuration);
        });
    }

    public static void HostUseGirvsConfig(
        this IConfigurationBuilder config,
        IWebHostEnvironment webHostEnvironment,
        string[] otherJsonFiles = null,
        string[] args = null
    )
    {
        //清除原有的源
        config.Sources.Clear();

        // 共享配置文件(GIRVS_SHARED_CONFIG 指向,由 Aspire AppHost 注入或 K8s ConfigMap 挂载):
        // 作为最低优先级配置源,服务本地 appsettings*.json 与环境变量天然覆盖其中的同名键。
        var sharedConfigPath = Environment.GetEnvironmentVariable("GIRVS_SHARED_CONFIG");

        if (string.IsNullOrWhiteSpace(sharedConfigPath))
            sharedConfigPath = "girvs.shared.json";

        config.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);

        config.AddJsonFile(ConfigurationDefaults.AppSettingsFilePath, true, true);
        if (otherJsonFiles is {Length: > 0})
        {
            foreach (var otherJsonFile in otherJsonFiles)
            {
                config.AddJsonFile(otherJsonFile, true, true);
            }
        }

        var appSettingsEnvironmentNameFilePath =
            ConfigurationDefaults.AppSettingsEnvironmentNameFilePath(
                webHostEnvironment.EnvironmentName
            );
        config.AddJsonFile(appSettingsEnvironmentNameFilePath, true, true);

        if (
            webHostEnvironment.IsDevelopment()
            && webHostEnvironment.ApplicationName is {Length: > 0}
        )
        {
            try
            {
                var appAssembly = Assembly.Load(
                    new AssemblyName(webHostEnvironment.ApplicationName)
                );
                config.AddUserSecrets(appAssembly, optional: true, reloadOnChange: true);
            }
            catch (FileNotFoundException)
            {
                // The assembly cannot be found, so just skip it.
            }
        }

        config.AddEnvironmentVariables();
        if (args is {Length: > 0})
            config.AddCommandLine(args);

        if (config is IConfiguration configuration)
        {
            configuration.ReplaceEnvironmentVariables(); // 自定义方法，替换占位符
        }
        else
        {
            var builtConfig = config.Build(); // 创建配置实例
            builtConfig.ReplaceEnvironmentVariables();
            config.Sources.Clear();
            config.AddConfiguration(builtConfig);
        }
    }

    /// <summary>
    /// 旧拼写兼容方法，转发到 CreateGirvsWebApplicationBuilder
    /// </summary>
    [Obsolete("拼写修正：请使用 CreateGirvsWebApplicationBuilder(args)")]
    public static WebApplication CreateGrivsWebApplicationBuilder(string[] args) =>
        CreateGirvsWebApplicationBuilder(args);

    public static WebApplication CreateGirvsWebApplicationBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.HostUseSerilog();
        builder.Configuration.HostUseGirvsConfig(builder.Environment, args);

        CommonHelper.DefaultFileProvider = new GirvsFileProvider(builder.Environment);
        var typeFinder = new WebAppTypeFinder();
        var startupType = typeFinder.FindOfType<IGirvsStartup>();

        var startups = new List<IGirvsStartup>();

        foreach (var type in startupType)
        {
            // 获取构造函数
            var constructor = type.GetConstructor(
                [typeof(IConfiguration), typeof(IWebHostEnvironment)]
            );

            if (constructor != null)
            {
                var parameters = new object[] {builder.Configuration, builder.Environment};

                if (constructor.Invoke(parameters) is not IGirvsStartup startup)
                    continue;
                startup.ConfigureServices(builder.Services);
                startups.Add(startup);
            }
            else
            {
                throw new GirvsException(
                    "Startup class must have a constructor with IConfiguration and IWebHostEnvironment parameters"
                );
            }
        }

        builder.Services.ConfigureApplicationServices(builder.Configuration, builder.Environment);

        var app = builder.Build();

        foreach (var startup in startups)
            startup.Configure(app, builder.Environment);

        app.ConfigureRequestPipeline(builder.Environment);
        app.ConfigureEndpointRouteBuilder();
        return app;
    }
}
