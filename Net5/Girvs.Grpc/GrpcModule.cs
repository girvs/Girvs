using System;
using Girvs.Infrastructure;
using Girvs.WebFrameWork.Plugins.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Grpc
{
    public class GrpcModule: IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<GirvsExceptionInterceptor>();
            });
        }

        public void Configure(IApplicationBuilder application)
        {
            application.UseGrpcWeb(new GrpcWebOptions() {DefaultEnabled = true});
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            builder.AddEndpointRouteBuilderGrpcServices();
        }

        public int Order { get; } = 99901;
    }
}