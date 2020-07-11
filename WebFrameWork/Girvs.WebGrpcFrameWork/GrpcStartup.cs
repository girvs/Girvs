using Girvs.Domain.Infrastructure;
using Girvs.WebGrpcFrameWork.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebGrpcFrameWork
{
    public class GrpcStartup : IGirvsStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSpGrpcService();
        }

        public void Configure(IApplicationBuilder application)
        {
            //静态文件
            application.UseStaticFiles();
            application.UseAutoGrpcServices();
        }

        public int Order { get; } = 10;
    }
}