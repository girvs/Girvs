using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Power.EventBus.Extensions
{
    public static class CapServiceExtensions
    {
        public static IServiceCollection AddCapConfigServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCap(x =>
            {
                x.UseDashboard();
                // Register to Consul
                //x.UseDiscovery(d =>
                //{
                //    var hostPort = configuration["ConsulConfig:ConsulAddress"];
                //    Uri uri = new Uri(hostPort);
                    
                //    d.DiscoveryServerHostName = uri.Host;
                //    d.DiscoveryServerPort = uri.Port;
                //    d.CurrentNodeHostName = configuration["ConsulConfig:CallBackAddress"];
                //    d.CurrentNodePort = int.Parse(configuration["ConsulConfig:CallBackAddressPort"]);
                //    d.NodeId = "Cap_Dashboard";
                //    d.NodeName = "CAP No.1 Node";
                //});
                x.UseMySql(configuration["Cap:DataConnectionString"]);
                x.UseRabbitMQ(options =>
                {
                    options.HostName = configuration["Cap:RabbitMQ:HostName"];
                    options.Port = int.Parse(configuration["Cap:RabbitMQ:Port"]);
                    options.UserName = configuration["Cap:RabbitMQ:UserName"];
                    options.Password = configuration["Cap:RabbitMQ:Password"];
                    options.VirtualHost = configuration["Cap:RabbitMQ:VirtualHost"];
                });
            });
            return services;
        }
    }
}