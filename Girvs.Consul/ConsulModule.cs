namespace Girvs.Consul;

public class ConsulModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var consulConfig = Singleton<AppSettings>.Instance.Get<ConsulConfig>();
            
        //需要添加判断是否存在GRPC服务
        if (consulConfig.CurrentServerModel == ServerModel.GrpcService)
        {
            consulConfig.ServerName = string.IsNullOrEmpty(consulConfig.ServerName)
                ? AppDomain.CurrentDomain.FriendlyName.Replace(".", "-").ToLower()
                : consulConfig.ServerName;

            var uri = new Uri(consulConfig.HealthAddress);
            services.AddConsul(new NConsulOptions
                {
                    Address = consulConfig.ConsulAddress,
                })
                .AddGRPCHealthCheck(consulConfig.HealthAddress.Replace($"{uri.Scheme}://", ""))
                .RegisterService(consulConfig.ServerName, uri.Host, uri.Port, new[] {".net Core GrpcService"});
        }
    }

    public void Configure(IApplicationBuilder application)
    {
        //需要添加判断是否存在WebApi服务
        application.UseConsulByWebApi();
    }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {

    }

    public int Order { get; } = 99999;
}