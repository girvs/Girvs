using Girvs.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Infrastructure.GrpcExtensions
{
    public class GrpcGirvsStartup : IGirvsStartup
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
            application.UseGrpcWeb(new GrpcWebOptions() { DefaultEnabled = true });
        }

        public void EndpointRouteBuilder(IEndpointRouteBuilder builder)
        {
            builder.AddEndpointRouteBuilderGrpcServices();
        }

        public int Order { get; } = 208;
    }
}