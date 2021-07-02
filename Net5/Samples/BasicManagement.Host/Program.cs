using Microsoft.Extensions.Hosting;
using Girvs;

namespace BasicManagement.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GirvsHostBuilderManager.CreateGrivsHostBuilder<Startup>(args).Build().Run();
        }
    }
}
