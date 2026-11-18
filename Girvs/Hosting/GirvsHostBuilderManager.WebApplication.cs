using Girvs.Infrastructure.Extensions;

namespace Girvs;

public static partial class GirvsHostBuilderManager
{
    public static WebApplication CreateGirvsWebApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.HostUseSerilog();
        builder.Configuration.HostUseGirvsConfig(builder.Environment, args);

        CommonHelper.DefaultFileProvider = new GirvsFileProvider(builder.Environment);
        var typeFinder = new WebAppTypeFinder();
        var startupType = typeFinder.FindOfType<IGirvsStartup>();

        var startups = new List<IGirvsStartup>();

        foreach (var type in startupType)
        {
            // 获取构造函数
            var constructor = type.GetConstructor([
                typeof(IConfiguration),
                typeof(IWebHostEnvironment),
            ]);

            if (constructor != null)
            {
                var parameters = new object[] { builder.Configuration, builder.Environment };

                if (constructor.Invoke(parameters) is not IGirvsStartup startup)
                    continue;
                startup.ConfigureServices(builder.Services);
                startups.Add(startup);
            }
            else
            {
                throw new GirvsException(
                    "Startup class must have a constructor with IConfiguration and IWebHostEnvironment parameters"
                );
            }
        }

        var (engine, _) = builder.Services.ConfigureApplicationServices(
            builder.Configuration,
            builder.Environment
        );

        var app = builder.Build();

        if (engine is GirvsEngine girvsEngine)
            girvsEngine.SetServiceProvider(app.Services);

        foreach (var startup in startups)
            startup.Configure(app, builder.Environment);

        app.ConfigureRequestPipeline(builder.Environment);
        app.ConfigureEndpointRouteBuilder();
        return app;
    }
}
