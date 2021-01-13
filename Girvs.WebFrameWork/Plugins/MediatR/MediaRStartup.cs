using System.Linq;
using FluentValidation;
using Girvs.Domain.Configuration;
using Girvs.Domain.Driven.Behaviors;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Plugins.MediatR
{
    public class MediaRStartup : IPluginStartup
    {
        public string Name { get; }= "MediatR";
        public bool Enabled
        {
            get
            {
                var config = EngineContext.Current.Resolve<GirvsConfig>();
                var funcConfig = config.FunctionalModules.SingleOrDefault(x => x.Name == Name);
                return funcConfig != null && funcConfig.Enabled;
            }
        }
        public void ConfigureServicesRegister(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddMediatR(typeof(MediaRStartup));
            services.RegisterType(typeof(INotificationHandler<>), typeFinder, asType: null);
            services.RegisterType(typeof(CommandHandler), typeFinder, asType: null);
            services.RegisterIValidatorType(typeof(IValidator), typeFinder);
            //添加验证管道
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CommandOperateBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

            //services.RegisterType(typeof(IPipelineBehavior<,>), typeFinder, asType: null);
        }

        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
            
        }

        public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
        {
            
        }

        public int Order { get; } = 108;
    }
}