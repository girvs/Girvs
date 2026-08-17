using Girvs.Configuration;
using Girvs.Infrastructure;
using Girvs.Infrastructure.Extensions;
using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace Sample.Gateway;

public class GatewayModule : IAppModuleStartup
{
    private const string EsResourceName = "Elasticsearch";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // var girvsResource = Singleton<AppSettings>.Instance.Resources[EsResourceName];
        // var esNodeUrl = girvsResource.Settings["NodeUrls"];
        // if (Uri.TryCreate(esNodeUrl, UriKind.Absolute, out var nodeUri))
        // {
        //     var deployName = Environment.GetEnvironmentVariable("DEPLOY_SYSTEM_NAME") ?? "girvs";
        //     var serverName = Environment.GetEnvironmentVariable("CURRENT_SERVER_NAME") ?? "gateway";
        //     var indexFormat = $"{deployName}-{serverName}-webapi-{{0:yyyy.MM.dd}}".ToLowerInvariant();
        //
        //     services.AddSerilogSink(
        //         cfg =>
        //             cfg.WriteTo.Elasticsearch(
        //                 new ElasticsearchSinkOptions(nodeUri)
        //                 {
        //                     IndexFormat = indexFormat,
        //                     AutoRegisterTemplate = true,
        //                     EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
        //                 }
        //             ),
        //         "Elasticsearch"
        //     );
        // }
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder) { }

    public int Order { get; } = 10888;
}
