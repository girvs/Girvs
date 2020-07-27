using Girvs.WebFrameWork;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Test.GrpcServiceWebHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SpHostBuilderManager.CreateSpHostBuilder<Startup>(args).Build().Run();
        }
    }
}
