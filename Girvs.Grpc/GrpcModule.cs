namespace Girvs.Grpc;

public class GrpcModule: IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<GirvsExceptionInterceptor>();
        });
    }

    public void Configure(IApplicationBuilder application)
    {
        application.UseGrpcWeb(new GrpcWebOptions() {DefaultEnabled = true});
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
        builder.AddEndpointRouteBuilderGrpcServices();
    }

    public int Order { get; } = 99901;
}