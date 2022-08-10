namespace Girvs.EventBus.Configuration;

public class EventBusConfig : IAppModuleConfig
{
    public EventBusType EventBusType { get; set; } = EventBusType.Kafka;

    public int ConsumerThreadCount { get; set; } = 1;

    public int ProducerThreadCount { get; set; } = 1;

    public DbType DbType { get; set; } = DbType.MySql;

    public string DbConnectionString { get; set; } =
        "Server=192.168.51.166;database=Wb_BasicManagement;User ID=root;Password=123456;Character Set=utf8;";

    public RabbitMQConfig RabbitMqConfig { get; set; } = new RabbitMQConfig();
    public KafkaConfig KafkaConfig { get; set; } = new KafkaConfig();
    public RedisConfig RedisConfig { get; set; } = new RedisConfig();

    public void Init()
    {
    }
}

public class RabbitMQConfig
{
    public string HostName { get; set; } = "amqp-cn-zvp2aqiah00e.mq-amqp.cn-shenzhen-429403-a.aliyuncs.com";

    public string Password { get; set; } =
        "NDFDRTEzNzIyNDYxNjY0Nzc4M0RFODZBNERCMDBDNDdBQjczMzdEMjoxNjI4NDcyOTEwMjE1";

    public string UserName { get; set; } = "MjphbXFwLWNuLXp2cDJhcWlhaDAwZTpMVEFJT2RmYmJ1S2FqdkQ1";
    public string VirtualHost { get; set; } = "zhuofan.wb";
    public string ExchangeName { get; set; } = "cap.default.router";
    public int Port { get; set; } = 5672;
}

public class KafkaConfig
{
    public string KafKaConnectionString = "120.79.199.188:9093,120.24.175.230:9093,120.79.90.169:9093";
    public string SslCaLocation { get; set; } = "./aliyunkafka.pem";
    public string SaslMechanism { get; set; } = "PLAIN";
    public string SecurityProtocol { get; set; } = "SASL_SSL";
    public string SaslUsername { get; set; } = "zhuofang";
    public string SaslPassword { get; set; } = "ZhuoFan168";
}

public class RedisConfig
{
    public string RedisConnectionString { get; set; }
}


public enum EventBusType
{
    RabbitMQ,
    Kafka,
    Redis
}

public enum DbType
{
    MsSql,
    MySql,
    SqlLite,
    Oracle
}