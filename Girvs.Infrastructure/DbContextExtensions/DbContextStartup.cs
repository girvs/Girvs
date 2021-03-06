﻿using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Infrastructure.DbContextExtensions
{
    public class DbContextStartup : IPluginStartup
    {
        public string Name { get; } = "DbContext";
        public bool Enabled { get; set; } = true;

        public void ConfigureServicesRegister(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddSpObjectContext();
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 1;
    }
}