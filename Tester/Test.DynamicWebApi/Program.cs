using Girvs.WebFrameWork;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Test.DynamicWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GirvsHostBuilderManager.CreateSpHostBuilder<Startup>(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
