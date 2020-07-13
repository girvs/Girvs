using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Girvs.WebFrameWork;

namespace SmartProducts.Person.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SpHostBuilderManager.CreateSpHostBuilder<Startup>(args).Build().Run();
            //CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
    }
}