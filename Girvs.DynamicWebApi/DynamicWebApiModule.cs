using System;
using Girvs.TypeFinder;

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
                if (Activator.CreateInstance(optionsActionType) is IDynamicWebApiModuleOptionsAction optionsAction)
                {
                    optionsAction.OptionsAction(options);
                }
            }
        });
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
    }

    public int Order { get; } = 4;
}