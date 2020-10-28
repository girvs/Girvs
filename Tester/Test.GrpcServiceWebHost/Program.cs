using Girvs.Service.FrameWork;
using Microsoft.Extensions.Hosting;

namespace Test.GrpcServiceWebHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GirvsHostBuilderManager.CreateSpHostBuilder<Startup>(args).Build().Run();
        }
    }
}
