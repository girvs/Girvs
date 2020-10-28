using Girvs.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Infrastructure.SwaggerExtensions
{
    public class SwaggerGirvsStartup : IGirvsStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerServices();
        }

        public void Configure(IApplicationBuilder application)
        {
            application.UseSwaggerService();
        }

        public void EndpointRouteBuilder(IEndpointRouteBuilder builder)
        {

        }

        public int Order { get; } = 202;
    }
}