using Girvs.Domain.TypeFinder;
using Girvs.WebFrameWork.Filters;
using Girvs.WebFrameWork.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Power.EventBus;
using Power.EventBus.Extensions;

namespace Power.BasicManagement.WebApi
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
            services.AddControllers(options =>
            {
                options.Filters.Add<PermissionFilter>(); //权限验证
            });

            services.ConfigureApplicationServices(Configuration, WebHostEnvironment);
            services.AddCapConfigServer(Configuration);
            var typeFinder = services.BuildServiceProvider().GetService<ITypeFinder>();
            services.RegisterType(typeof(IIntegrationEventHandler<>), typeFinder, false);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSendAuthorizePermission();

            if (WebHostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseGirvsExceptionHandler();
            app.ConfigureRequestPipeline();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.ConfigureEndpointRouteBuilder();
            });
        }
    }
}