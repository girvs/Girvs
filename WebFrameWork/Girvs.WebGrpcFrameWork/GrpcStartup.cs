using Girvs.Domain.Configuration;
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
            var girvsConfig = EngineContext.Current.Resolve<GirvsConfig>();
            if (girvsConfig.CurrentServerModel == ServerModel.GrpcService)
            {
                services.AddSpGrpcService();
            }
        }

        public void Configure(IApplicationBuilder application)
        {
            var girvsConfig = EngineContext.Current.Resolve<GirvsConfig>();
            if (girvsConfig.CurrentServerModel == ServerModel.GrpcService)
            {
                application.UseAutoGrpcServices();
            }
        }

        public int Order { get; } = 10;
    }
}