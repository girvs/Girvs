using System;
using System.IO;
using Girvs.WebFrameWork;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Power.BasicManagement.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GirvsHostBuilderManager.CreateGrivsHostBuilder<Startup>(args).Build().Run();
        }

        public static IHostBuilder CreateGrivsHostBuilder<TStartup>(string[] args) where TStartup : class
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string spConfigFile = Path.Combine(basePath, "sp.json");
            string consulConfigFile = Path.Combine(basePath, "consul.json");
            string serilogConfigFile = Path.Combine(basePath, "serilog.json");
            string identityServerConfigFile = Path.Combine(basePath, "identityserver.json");
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

        public static IHostBuilder CreateGrivsHostBuilder<TStartup>(string[] args, IConfigurationRoot configuration)
            where TStartup : class
        {
            return Host.CreateDefaultBuilder(args)
                .UseConsoleLifetime()
                .UseSerilog((context, config) => { config.ReadFrom.Configuration(context.Configuration); }).
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration(configurationBuilder =>
                    {
                        configurationBuilder.AddConfiguration(configuration);
                    });

                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(1910, o => o.Protocols = HttpProtocols.Http1AndHttp2);
                        //options.ListenAnyIP(1810, o => o.Protocols = HttpProtocols.Http1AndHttp2);
                    });
                    webBuilder.UseStartup<TStartup>();
                });
        }
    }
}
