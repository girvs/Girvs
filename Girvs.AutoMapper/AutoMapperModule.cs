using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Girvs.AutoMapper;

public class AutoMapperModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var typeFinder = new WebAppTypeFinder();
        var mapperConfigurations = typeFinder.FindOfType<IOrderedMapperProfile>();

        var instances = mapperConfigurations
            .Select(mapperConfiguration =>
                (IOrderedMapperProfile)Activator.CreateInstance(mapperConfiguration)
            )
            .OrderBy(mapperConfiguration => mapperConfiguration.Order);

        var logFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();

        var config = new MapperConfiguration(
            cfg =>
            {
                //cfg.AddProfile<DefaultProfile>();
                foreach (var instance in instances)
                {
                    cfg.AddProfile(instance.GetType());
                }
            },
            logFactory
        );
        services.AddSingleton(typeof(IMapper), config.CreateMapper());
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; }
}
