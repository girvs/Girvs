using System;
using System.IO;
using Girvs.WebFrameWork.Plugins;
using Girvs.WebFrameWork.Plugins.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Panda.DynamicWebApi;

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
            //app.UseSwagger();
            //app.UseSwaggerUI(c =>
            //{
            //    c.SwaggerEndpoint("/swagger/v1/swagger.json", AppDomain.CurrentDomain.FriendlyName);
            //});

            //app.UseRouting();

            //app.UseAuthorization();

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapControllers();
            //});


            if (WebHostEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //¾²Ì¬ÎÄ¼þ
            app.UseStaticFiles();
            app.ConfigureRequestPipeline();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.ConfigureEndpointRouteBuilder();
            });
        }
    }
}
