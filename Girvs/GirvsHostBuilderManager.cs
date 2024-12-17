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
                    .ConfigureAppConfiguration(config =>
                    {
                        config
                            .AddJsonFile(ConfigurationDefaults.AppSettingsFilePath, true, true)
                            .AddJsonFile(ConfigurationDefaults.SerilogSettingFilePath, true, true)
                            .AddJsonFile("./config/appsettings.Production.json", true, true)
                            .AddEnvironmentVariables()
                            .AddCommandLine(args);
                    })
                    .UseStartup<TStartup>();
            });
    }
}
