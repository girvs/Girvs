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
                        (hostingContext, appConfigBuilder) =>
                        {
                            //清除源有的源
                            appConfigBuilder.Sources.Clear();

                            var reloadOnChange = hostingContext.Configuration.GetValue(
                                "hostBuilder:reloadConfigOnChange",
                                defaultValue: true
                            );
                            var env = hostingContext.HostingEnvironment;

                            appConfigBuilder
                                .AddJsonFile(
                                    ConfigurationDefaults.AppSettingsFilePath,
                                    true,
                                    reloadOnChange
                                )
                                .AddJsonFile(
                                    ConfigurationDefaults.SerilogSettingFilePath,
                                    true,
                                    reloadOnChange
                                )
                                .AddJsonFile(
                                    ConfigurationDefaults.AppSettingsEnvironmentNameFilePath(
                                        env.EnvironmentName
                                    ),
                                    optional: true,
                                    reloadOnChange: reloadOnChange
                                );

                            if (env.IsDevelopment() && env.ApplicationName is { Length: > 0 })
                            {
                                try
                                {
                                    var appAssembly = Assembly.Load(
                                        new AssemblyName(env.ApplicationName)
                                    );
                                    appConfigBuilder.AddUserSecrets(
                                        appAssembly,
                                        optional: true,
                                        reloadOnChange: reloadOnChange
                                    );
                                }
                                catch (FileNotFoundException)
                                {
                                    // The assembly cannot be found, so just skip it.
                                }
                            }

                            appConfigBuilder.AddEnvironmentVariables();

                            if (args is { Length: > 0 })
                                appConfigBuilder.AddCommandLine(args);

                            // 获取配置并执行替换操作
                            var builtConfig = appConfigBuilder.Build(); // 创建配置实例

                            builtConfig.ReplaceEnvironmentVariables(); // 自定义方法，替换占位符

                            appConfigBuilder.AddConfiguration(builtConfig);
                        }
                    )
                    .UseStartup<TStartup>();
            });
    }
}
