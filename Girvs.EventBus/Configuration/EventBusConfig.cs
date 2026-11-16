namespace Girvs.EventBus.Configuration;

public class EventBusConfig : IAppModuleConfig
{
    /// <summary>
    /// 持久化资源引用：索引 0 为 Resources 资源键格式串，索引 1 起为 string.Format 参数；
    /// 同一组参数既用于组合资源键，也用于格式化资源的 Settings:ConnectionString。
    /// 例如 ["common-mysql", "girvs_event_test"] 会引用资源 common-mysql，
    /// 并把 ConnectionString 模板中的 {0} 替换为 girvs_event_test。
    /// </summary>
    public string[] PersistenceConnectionRef { get; set; }

    /// <summary>
    /// 传输资源引用，语义与 <see cref="PersistenceConnectionRef"/> 相同；
    /// 引用参数用于格式化资源的 Settings:Endpoints（Redis）
    /// 或 Settings:ConnectionString（RabbitMQ/Kafka）。
    /// </summary>
    public string[] TransportConnectionRef { get; set; }

    public int ConsumerThreadCount { get; set; } = 1;
    public int ProducerThreadCount { get; set; } = 1;
    public int SucceedMessageExpiredAfter { get; set; } = 60;
    public int FailedMessageExpiredAfter { get; set; } = 15 * 24 * 3600;

    /// <summary>
    /// 是否启用 CAP Dashboard。Dashboard 路由不提供任何鉴权，
    /// 开启后会把消息查询与重发能力暴露给所有可访问该端口的调用方，
    /// 因此默认关闭；确需使用时请在外层网关或中间件限制访问。
    /// </summary>
    public bool EnableDashboard { get; set; }

    public void Init() { }
}
