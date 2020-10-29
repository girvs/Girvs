using Girvs.WebFrameWork.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Test.DynamicWebApi
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private IWebHostEnvironment WebHostEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.ConfigureApplicationServices(Configuration, WebHostEnvironment);
        }

        public void Configure(IApplicationBuilder app)
        {
            //¾²Ì¬ÎÄ¼þ
            app.UseStaticFiles();
            app.ConfigureRequestPipeline();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.ConfigureEndpointRouteBuilder();
            });
        }
    }
}
