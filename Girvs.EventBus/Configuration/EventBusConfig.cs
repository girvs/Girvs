using System.Data;
using Girvs.Configuration;

namespace Girvs.EventBus.Configuration
{
    public class EventBusConfig : IAppModuleConfig
    {
        public EventBusType EventBusType { get; set; } = EventBusType.Cap;

        public DbType DbType { get; set; } = DbType.MySql;

        public string DbConnectionString { get; set; } = "Server=192.168.51.166;database=Wb_BasicManagement;User ID=root;Password=123456;Character Set=utf8;";

        public string RabbitMqConnectionString { get; set; } =
            "host=192.168.51.98;virtualHost=mike;username=test;password=123456";

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