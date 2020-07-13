using Girvs.WebFrameWork.Infrastructure.ServicesExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartProducts.Person.WebApi
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
            //services.AddDbContext<PersonDbContext>(provider =>
            //{
            //    provider.UseSqlServer("Data Source=www.72de.net;Initial Catalog=HdytPerson;User Id=sa;Password=System00;Max Pool Size=500;Min Pool Size=1");

            //});
            services.ConfigureApplicationServices(Configuration, WebHostEnvironment);
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
        {
            application.ConfigureRequestPipeline();
        }
    }
}
