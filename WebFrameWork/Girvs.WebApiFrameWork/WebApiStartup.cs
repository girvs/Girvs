﻿using Girvs.Domain.Infrastructure;
using Girvs.WebApiFrameWork.Infrastructure;
using Girvs.WebFrameWork.Infrastructure.SwaggerExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebApiFrameWork
{
    public class WebApiStartup : IGirvsStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers(options =>
            {
               // options.Filters.Add<CustomExceptionAttribute>();
            });
            services.ConfigureSwaggerServices();
        }

        public void Configure(IApplicationBuilder application)
        {
            application.UseSpExceptionHandler();
            //静态文件
            application.UseStaticFiles();
            application.UseCustomSwagger();
            application.UseRouting();
            application.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public int Order { get; } = 100;
    }
}