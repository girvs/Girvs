using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.WebApiFrameWork.Infrastructure;
using Girvs.WebFrameWork.Infrastructure.SwaggerExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Panda.DynamicWebApi;

namespace Girvs.WebApiFrameWork
{
    public class WebApiStartup : IGirvsStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureSwaggerServices();
            var girvsConfig = EngineContext.Current.Resolve<GirvsConfig>();
            if (girvsConfig.CurrentServerModel == ServerModel.WebApi)
            {
                services.AddControllers(options =>
                {
                    // options.Filters.Add<CustomExceptionAttribute>();
                });
            }

            if (girvsConfig.DynamicWebApiEnable)
            {
                services.AddDynamicWebApi();
            }
        }

        public void Configure(IApplicationBuilder application)
        {
            var girvsConfig = EngineContext.Current.Resolve<GirvsConfig>();
            if (girvsConfig.CurrentServerModel == ServerModel.WebApi)
            {
                application.UseSpExceptionHandler();
                //静态文件
                application.UseStaticFiles();
                application.UseCustomSwagger();
                application.UseRouting();
                application.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            }
        }

        public int Order { get; } = 100;
    }
}