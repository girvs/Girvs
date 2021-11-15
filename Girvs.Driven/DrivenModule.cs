using System;
using System.Reflection;
using Girvs.Driven.Behaviors;
using Girvs.Driven.Extensions;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Driven
{
    public class DrivenModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var typeFinder = new WebAppTypeFinder();
            services.AddMediatR(configuration =>
            {
                configuration.AsScoped();
            },typeof(DrivenModule));
            services.RegisterNotificationHandlerType();
            services.RegisterCommandHandlerType();
            services.RegisterIValidatorType();
            // services.RegisterType(typeof(INotificationHandler<>), typeFinder, asType: null);
            // services.RegisterType(typeof(CommandHandler), typeFinder, asType: null);
            // services.RegisterIValidatorType(typeof(IValidator), typeFinder);
            // //添加验证管道
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CommandOperateBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
        }

        public void Configure(IApplicationBuilder application)
        {
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
        }

        public int Order { get; } = 3;
    }
}