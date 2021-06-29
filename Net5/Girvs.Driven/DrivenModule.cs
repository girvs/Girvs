using Girvs.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Driven
{
    
    public class DrivenModule:IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
             services.AddMediatR(typeof(DrivenModule));
            // services.RegisterType(typeof(INotificationHandler<>), typeFinder, asType: null);
            // services.RegisterType(typeof(CommandHandler), typeFinder, asType: null);
            // services.RegisterIValidatorType(typeof(IValidator), typeFinder);
            // //添加验证管道
            // services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CommandOperateBehavior<,>));
            // services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
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