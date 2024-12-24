using Girvs.Infrastructure.Extensions;
using Serilog;

namespace Girvs;

public static class GirvsHostBuilderManager
{
    public static IHostBuilder CreateGrivsHostBuilder<TStartup>(string[] args)
        where TStartup : class
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.HostUseSerilog();
        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder
                .ConfigureAppConfiguration(
                    (hostingContext, appConfigBuilder) =>
                        //清除源有的源
                        appConfigBuilder.HostUseGirvsConfig(hostingContext.HostingEnvironment, args)
                )
                .UseStartup<TStartup>();
        });

        return builder;
    }

    public static void HostUseSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog(
            (context, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
            }
        );
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
        config.AddJsonFile(ConfigurationDefaults.AppSettingsFilePath, true, true);
        config.AddJsonFile(ConfigurationDefaults.SerilogSettingFilePath, true, true);
        if (otherJsonFiles is { Length: > 0 })
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
            && webHostEnvironment.ApplicationName is { Length: > 0 }
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
        if (args is { Length: > 0 })
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

    public static WebApplication CreateGrivsWebApplicationBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.HostUseSerilog();
        builder.Configuration.HostUseGirvsConfig(builder.Environment, args);

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
                var parameters = new object[] { builder.Configuration, builder.Environment };

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
