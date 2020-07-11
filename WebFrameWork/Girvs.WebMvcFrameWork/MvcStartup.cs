using Girvs.Domain.Infrastructure;
using Girvs.WebFrameWork.Infrastructure.AntiForgeryExtensions;
using Girvs.WebFrameWork.Infrastructure.HttpSessionExtensions;
using Girvs.WebFrameWork.Infrastructure.ServicesExtensions;
using Girvs.WebFrameWork.Infrastructure.SwaggerExtensions;
using Girvs.WebMvcFrameWork.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebMvcFrameWork
{
    public class MvcStartup : IGirvsStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllersWithViews();
            services.AddHttpSession();
            services.AddSpAntiforgery();
            services.ConfigureSwaggerServices();
        }

        public void Configure(IApplicationBuilder application)
        {
            application.UseSpMvcExceptionHandler();
            //静态文件
            application.UseStaticFiles();
            application.UseBadRequestResult();
            application.UsePageNotFound();
            application.UseCustomSwagger();
            application.UseRouting();
            application.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public int Order { get; } = 100;
    }
}