using Serilog;

namespace Girvs;

public static class GirvsHostBuilderManager
{
    public static IHostBuilder CreateGrivsHostBuilder<TStartup>(string[] args)
        where TStartup : class
    {
        return Host.CreateDefaultBuilder(args)
            .UseConsoleLifetime()
            .UseSerilog(
                (context, config) =>
                {
                    config.ReadFrom.Configuration(context.Configuration);
                }
            )
            .UseDefaultServiceProvider(options =>
            {
                //we don't validate the scopes, since at the app start and the initial configuration we need
                //to resolve some services (registered as "scoped") through the root container
                options.ValidateScopes = false;
                options.ValidateOnBuild = true;
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .ConfigureAppConfiguration(
                        (hostingContext, config) =>
                        {
                            var reloadOnChange = hostingContext.Configuration.GetValue(
                                "hostBuilder:reloadConfigOnChange",
                                defaultValue: true
                            );

                            config.AddJsonFile(
                                ConfigurationDefaults.SerilogSettingFilePath,
                                true,
                                reloadOnChange
                            );

                            // 获取配置并执行替换操作
                            var builtConfig = config.Build(); // 创建配置实例

                            builtConfig.ReplaceEnvironmentVariables(); // 自定义方法，替换占位符

                            config.AddConfiguration(builtConfig);
                        }
                    )
                    .UseStartup<TStartup>();
            });
    }
}
