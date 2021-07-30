using Girvs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ZhuoFan.Wb.BasicService.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GirvsHostBuilderManager.CreateGrivsHostBuilder<Startup>(args).Build().Run();
        }
    }
}
