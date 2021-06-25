using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Girvs.WebFrameWork
{
    public class GirvsHostBuilderManager
    {
        public static IHostBuilder CreateGrivsHostBuilder<TStartup>(string[] args) where TStartup : class
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var spConfigFile = Path.Combine(basePath, "sp.json");
            var consulConfigFile = Path.Combine(basePath, "consul.json");
            var serilogConfigFile = Path.Combine(basePath, "serilog.json");
            var identityServerConfigFile = Path.Combine(basePath, "identityserver.json");
            var config = new ConfigurationBuilder()
                .AddJsonFile(spConfigFile, true, true)
                .AddJsonFile(consulConfigFile, true, true)
                .AddJsonFile(serilogConfigFile, true, true)
                .AddJsonFile(identityServerConfigFile, true, true)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.Development.json", true, true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
            return CreateGrivsHostBuilder<TStartup>(args, config);
        }

        private static IHostBuilder CreateGrivsHostBuilder<TStartup>(string[] args, IConfiguration configuration)
            where TStartup : class
        {
            return Host.CreateDefaultBuilder(args)
                .UseConsoleLifetime()
                .UseSerilog((context, config) => { config.ReadFrom.Configuration(context.Configuration); })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration(configurationBuilder =>
                        configurationBuilder.AddConfiguration(configuration));
                    webBuilder.UseStartup<TStartup>();
                });
        }
    }
}