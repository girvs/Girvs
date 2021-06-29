using Girvs.Configuration;

namespace Girvs.Cap.Configuration
{
    public class CapConfig : IAppModuleConfig
    {
        public bool UseDashboard { get; set; } = true;
        public StoreType StoreType { get; set; } = StoreType.SqlServer;
        public string DataConnectionString { get; set; } = "";
        public MessageMiddleware MessageMiddleware { get; set; } = MessageMiddleware.RabbitMQ;
        public string MqConnectionString { get; set; } = "";
        public void Init()
        {
            
        }
    }


    public enum StoreType
    {
        SqlServer,
        MySql,
        PostgreSql,
        MongoDB
    }

    public enum MessageMiddleware
    {
        Kafka,
        RabbitMQ,
        AzureServiceBus,
        AmazonSQS
    }
}