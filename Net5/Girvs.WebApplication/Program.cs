using Girvs.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Girvs.WebApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseDefaultServiceProvider(options =>
                {
                    //we don't validate the scopes, since at the app start and the initial configuration we need 
                    //to resolve some services (registered as "scoped") through the root container
                    options.ValidateScopes = false;
                    options.ValidateOnBuild = true;
                })
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .ConfigureAppConfiguration(config =>
                    {
                        config
                            .AddJsonFile(ConfigurationDefaults.AppSettingsFilePath, true, true)
                            .AddEnvironmentVariables();
                    })
                    .UseStartup<Startup>());
            return host;
        }
    }
}