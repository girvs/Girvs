using Girvs.Configuration;

namespace Girvs.EventBus.Configuration
{
    public class EventBusConfig : IAppModuleConfig
    {
        public EventBusType EventBusType { get; set; } = EventBusType.Cap;

        public DbType DbType { get; set; } = DbType.MySql;

        public string DbConnectionString { get; set; } =
            "Server=192.168.51.166;database=Wb_BasicManagement;User ID=root;Password=123456;Character Set=utf8;";

        public string HostName { get; set; } = "192.168.51.98";
        public string Password { get; set; } = "123456";
        public string UserName { get; set; } = "test";
        public string VirtualHost { get; set; } = "mike";
        public string ExchangeName { get; set; } = "cap.default.router";
        public int Port { get; set; } = 5672;

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
        SqlLite,
        Oracle
    }
}