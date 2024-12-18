namespace Girvs.EventBus.Configuration;

public class EventBusConfig : IAppModuleConfig
{
    public EventBusType EventBusType { get; set; } = EventBusType.Kafka;

    /// <summary>
    /// 消费者线程并行处理消息的线程数，当这个值大于1时，将不能保证消息执行的顺序。
    /// </summary>
    public int ConsumerThreadCount { get; set; } = 1;

    /// <summary>
    ///
    /// </summary>
    public int ProducerThreadCount { get; set; } = 1;

    /// <summary>
    /// 成功消息的过期时间（秒）。 当消息发送或者消费成功时候，在时间达到 SucceedMessageExpiredAfter 秒时候将会从 Persistent 中删除，你可以通过指定此值来设置过期的时间
    /// </summary>
    public int SucceedMessageExpiredAfter { get; set; } = 1 * 60;

    /// <summary>
    /// 失败消息的过期时间（秒）。 当消息发送或者消费失败时候，在时间达到 FailedMessageExpiredAfter 秒时候将会从 Persistent 中删除，你可以通过指定此值来设置过期的时间。
    /// </summary>
    public int FailedMessageExpiredAfter { get; set; } = 15 * 24 * 3600;
    public DbType DbType { get; set; } = DbType.MySql;

    public string DbConnectionString { get; set; } =
        "Server=192.168.51.166;database=Wb_BasicManagement;User ID=root;Password=123456;Character Set=utf8;";

    public RabbitMQConfig RabbitMqConfig { get; set; } = new RabbitMQConfig();
    public KafkaConfig KafkaConfig { get; set; } = new KafkaConfig();
    public RedisConfig RedisConfig { get; set; } = new RedisConfig();

    public void Init() { }
}

public class RabbitMQConfig
{
    public string HostName { get; set; } =
        "amqp-cn-zvp2aqiah00e.mq-amqp.cn-shenzhen-429403-a.aliyuncs.com";

    public string Password { get; set; } =
        "NDFDRTEzNzIyNDYxNjY0Nzc4M0RFODZBNERCMDBDNDdBQjczMzdEMjoxNjI4NDcyOTEwMjE1";

    public string UserName { get; set; } = "MjphbXFwLWNuLXp2cDJhcWlhaDAwZTpMVEFJT2RmYmJ1S2FqdkQ1";
    public string VirtualHost { get; set; } = "zhuofan.wb";
    public string ExchangeName { get; set; } = "cap.default.router";
    public int Port { get; set; } = 5672;
}

public class KafkaConfig
{
    public string KafKaConnectionString =
        "120.79.199.188:9093,120.24.175.230:9093,120.79.90.169:9093";
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
