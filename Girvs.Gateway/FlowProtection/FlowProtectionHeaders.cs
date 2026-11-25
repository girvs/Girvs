namespace Girvs.Gateway.FlowProtection;

/// <summary>
/// 流程防护使用的请求头与响应头名称常量。
/// </summary>
/// <remarks>
/// 这些名称构成网关与客户端之间的公开契约。其中 <see cref="BusinessIdFromResponse"/>
/// 仅用于下游向网关回传业务标识，网关会在写出响应前将其剥离，不透传给客户端。
/// </remarks>
public static class FlowProtectionHeaders
{
    /// <summary>流程票据头：请求携带表示推进既有流程，响应回传表示本次流程未完成时的续用凭据。</summary>
    public const string Ticket = "X-Flow-Ticket";

    /// <summary>下一个允许执行的顺位，仅在流程未完成时回传。</summary>
    public const string NextIndex = "X-Flow-Next-Index";

    /// <summary>流程完成标记，值为 <c>true</c> 时表示该流程已无后续步骤。</summary>
    public const string Completed = "X-Flow-Completed";

    /// <summary>业务标识请求头，用于把票据绑定到具体业务对象。</summary>
    public const string BusinessId = "X-Business-Id";

    /// <summary>
    /// 下游回传业务标识专用的响应头。供下游在业务侧确定标识后交给网关落库，网关消费后立即移除，
    /// 不向客户端暴露。
    /// </summary>
    public const string BusinessIdFromResponse = "X-Flow-Business-Id";

    /// <summary>
    /// 需要暴露给浏览器脚本的响应头集合，供宿主在跨域配置中通过 <c>WithExposedHeaders</c> 引用。
    /// </summary>
    public static readonly string[] ResponseHeaders =
    [
        Ticket,
        NextIndex,
        Completed,
    ];
}
