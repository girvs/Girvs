namespace Girvs.Consul.Configuration;

public class ConsulConfig : IAppModuleConfig
{
    public string ServerName { get; set; } = "";
    public string ConsulAddress { get; set; } = "http://127.0.0.1:8500";
    public string HealthAddress { get; set; } = "http://127.0.0.1/Health";
    public int Interval { get; set; } = 10;
    public int DeregisterCriticalServiceAfter { get; set; } = 90;
    public int Timeout { get; set; } = 30;

    public ServerModel CurrentServerModel { get; set; } = ServerModel.WebApi;

    public void Init()
    {
    }
}


public enum ServerModel
{
    WebApi,
    GrpcService,
    Mvc
}