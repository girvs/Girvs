using System.Data;
using Girvs.Configuration;

namespace Girvs.EventBus.Configuration
{
    public class EventBusConfig : IAppModuleConfig
    {
        public EventBusType EventBusType { get; set; }
        
        public DbType DbType { get; set; } 

        public string DbConnectionString { get; set; }

        public string RabbitMqConnectionString { get; set; }

        public void Init()
        {
        }
    }


    public enum EventBusType
    {
        Cap,
        Dapr
    }

    public enum DbType
    {
        MsSql,
        MySql,
        Oracle,
        SqlLite
    }
}