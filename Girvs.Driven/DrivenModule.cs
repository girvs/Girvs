using Microsoft.AspNetCore.Hosting;

namespace Girvs.Driven;

public class DrivenModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var typeFinder = new WebAppTypeFinder();

#if NET9_0_OR_GREATER
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<DrivenModule>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });
#else
        services.AddMediatR(
            cfg => cfg.AsScoped(),
            typeof(DrivenModule));
#endif
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

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env)
    {
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
    }

    public int Order { get; } = 3;
}