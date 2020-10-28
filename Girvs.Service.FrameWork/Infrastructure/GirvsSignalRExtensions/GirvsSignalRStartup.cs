using Girvs.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Infrastructure.GirvsSignalRExtensions
{
    public class GirvsSignalRStartup : IGirvsStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSignalRCore();
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public void EndpointRouteBuilder(IEndpointRouteBuilder builder)
        {
            builder.AutoMapSignalREndpointRouteBuilder();
        }

        public int Order { get; } = 201;
    }
}