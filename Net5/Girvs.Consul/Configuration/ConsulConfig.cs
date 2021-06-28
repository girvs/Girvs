namespace Girvs.Consul.Configuration
{
    public class ConsulConfig
    {
        public string ServerName { get; set; }
        public string ConsulAddress { get; set; }
        public string HealthAddress { get; set; }
        public int Interval { get; set; }
        public int DeregisterCriticalServiceAfter { get; set; }
        public int Timeout { get; set; }

        public ServerModel CurrentServerModel { get; set; } = ServerModel.WebApi;
    }


    public enum ServerModel
    {
        WebApi,
        GrpcService,
        Mvc
    }
}
