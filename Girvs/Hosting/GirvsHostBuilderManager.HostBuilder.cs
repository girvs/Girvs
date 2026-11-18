namespace Girvs;

public static partial class GirvsHostBuilderManager
{
    public static IHostBuilder CreateGirvsHostBuilder<TStartup>(string[] args)
        where TStartup : class, IGirvsStartup
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
}
