namespace Girvs.Gateway.FlowProtection.Configuration;

/// <summary>
/// 网关流程防护配置（对应配置节 <c>FlowProtection</c>）。
/// </summary>
/// <remarks>
/// 该配置在启动期被 <see cref="FlowDefinitionRegistry"/> 校验并编译为只读流程定义，
/// 运行期不再重新读取；因此修改配置需要重启网关进程才能生效。
/// </remarks>
public sealed class FlowProtectionOptions
{
    /// <summary>
    /// 是否启用流程防护。为 <see langword="false" /> 时中间件直接放行，
    /// 且不注册 Redis 状态存储与响应头转换。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 流程票据的默认存活时长（秒），未在单条流程上覆盖时使用。默认 300 秒，允许范围 1 到 1800。
    /// </summary>
    public int DefaultTtlSeconds { get; set; } = 300;

    /// <summary>
    /// 单个流程步骤的处理锁时长（秒）。默认 30 秒，必须大于 0 且不超过 <see cref="DefaultTtlSeconds"/>。
    /// </summary>
    /// <remarks>
    /// 该锁用于防止同一顺位被并发重复执行；持锁进程异常退出时无需人工干预，
    /// 锁会在到期后自然失效，允许该步骤被重新尝试。
    /// </remarks>
    public int LockTtlSeconds { get; set; } = 30;

    /// <summary>
    /// 流程完成态的保留时长（秒），用于让客户端在完成后的一段时间内仍能收到“已完成”语义的拒绝。
    /// 默认 60 秒，允许范围 1 到 1800。
    /// </summary>
    public int CompletedTtlSeconds { get; set; } = 60;

    /// <summary>
    /// 流程票据所在的请求头名。默认 <c>X-Flow-Ticket</c>，同时决定响应头中回传票据的字段名。
    /// </summary>
    public string TicketHeaderName { get; set; } = FlowProtectionHeaders.Ticket;

    /// <summary>
    /// 业务标识所在的请求头名。默认 <c>X-Business-Id</c>，用于把票据绑定到具体业务对象。
    /// </summary>
    public string BusinessIdHeaderName { get; set; } = FlowProtectionHeaders.BusinessId;

    /// <summary>
    /// 流程定义列表；每项可配置为线性流程或参数化流程，<c>FlowId</c> 必须全局唯一。
    /// </summary>
    public List<FlowDefinitionOptions> Flows { get; set; } = [];
}

/// <summary>
/// 单条流程的配置项，由 <see cref="FlowDefinitionRegistry"/> 校验后编译为 <see cref="FlowDefinition"/>。
/// </summary>
/// <remarks>
/// <see cref="Steps"/> 与 <see cref="ParameterHeaderName"/> / <see cref="Interfaces"/> /
/// <see cref="Variants"/> 两组配置互斥，同一流程只能选用其中一组，混用会在启动期直接抛异常。
/// </remarks>
public sealed class FlowDefinitionOptions
{
    /// <summary>流程标识，全局唯一（忽略大小写）。</summary>
    public string FlowId { get; set; } = string.Empty;

    /// <summary>
    /// 该流程票据的存活时长（秒）；为 <see langword="null" /> 时回退到
    /// <see cref="FlowProtectionOptions.DefaultTtlSeconds"/>。
    /// </summary>
    public int? TtlSeconds { get; set; }

    /// <summary>
    /// 线性流程的有序步骤序列，至少两步。与参数化配置互斥。
    /// </summary>
    public List<FlowStepOptions> Steps { get; set; } = [];

    /// <summary>
    /// 前置参数所在的请求头名；非空即表示该流程为参数化流程。不得与系统保留头冲突。
    /// </summary>
    public string ParameterHeaderName { get; set; } = string.Empty;

    /// <summary>
    /// 命名接口池，供流程组合按名称引用；每个接口至少要被一条流程组合引用。
    /// </summary>
    public List<FlowInterfaceOptions> Interfaces { get; set; } = [];

    /// <summary>
    /// 前置参数取值到有序流程组合的映射：键为组合键，值为按顺序排列的接口名列表。
    /// </summary>
    public Dictionary<string, List<string>> Variants { get; set; } = [];
}

/// <summary>
/// 参数化流程接口池中的一个命名接口定义。
/// </summary>
public sealed class FlowInterfaceOptions
{
    /// <summary>
    /// 接口名，供 <see cref="FlowDefinitionOptions.Variants"/> 引用；
    /// 同一流程内去除首尾空白后忽略大小写唯一。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>HTTP 方法，必须是受支持的 HTTP 谓词；配置后统一规范化为大写。</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// 下游服务路径（不含网关服务名前缀），必须以 <c>/</c> 开头。
    /// 支持静态路径和整段 <c>{param}</c> 模板，匹配时静态路径优先。
    /// </summary>
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// 线性流程中的一个步骤定义。
/// </summary>
public sealed class FlowStepOptions
{
    /// <summary>HTTP 方法，必须是受支持的 HTTP 谓词；配置后统一规范化为大写。</summary>
    public string Method { get; set; } = string.Empty;

    /// <inheritdoc cref="FlowInterfaceOptions.Path" />
    public string Path { get; set; } = string.Empty;
}
