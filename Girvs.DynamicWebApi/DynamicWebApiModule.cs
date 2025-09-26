using System;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Hosting;

namespace Girvs.DynamicWebApi;

public class DynamicWebApiModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDynamicWebApi(options =>
        {
            var typeFinder = new WebAppTypeFinder();
            var optionsActionTypes = typeFinder.FindOfType<IDynamicWebApiModuleOptionsAction>();
            foreach (var optionsActionType in optionsActionTypes)
            {
                if (
                    Activator.CreateInstance(optionsActionType)
                    is IDynamicWebApiModuleOptionsAction optionsAction
                )
                {
                    optionsAction.OptionsAction(options);
                }
            }
        });
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
#if NET9_0
        var typeFinder = new WebAppTypeFinder();
        var serviceTypes = typeFinder.FindOfType<IAppWebMiniApiService>();
        foreach (var serviceType in serviceTypes)
        {
            if (Activator.CreateInstance(serviceType) is IAppWebMiniApiService service)
            {
                service.MapServiceMiniApi(builder);
            }
        }
#endif
    }

    public int Order { get; } = 4;
}
