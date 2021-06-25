using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Infrastructure
{
    public interface IEngine
    {
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        void ConfigureRequestPipeline(IApplicationBuilder application);

        void ConfigureEndpointRouteBuilder(IEndpointRouteBuilder endpointRouteBuilder);

        T Resolve<T>(IServiceScope scope = null) where T : class;

        object Resolve(Type type, IServiceScope scope = null);

        IEnumerable<T> ResolveAll<T>();

        object ResolveUnregistered(Type type);
    }
}