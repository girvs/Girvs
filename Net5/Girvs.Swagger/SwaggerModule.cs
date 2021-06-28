using System;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Swagger
{
    public class SwaggerModule: IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public void Configure(IApplicationBuilder application)
        {
            throw new NotImplementedException();
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            throw new NotImplementedException();
        }

        public int Order { get; }
    }
}