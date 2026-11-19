namespace Girvs.Driven.Events;

public record Message(string MessageType, Guid AggregateId, MessageSource MessageSource)
    : Message<bool>(MessageType, AggregateId, MessageSource)
{
    public Message()
        : this(string.Empty, Guid.Empty, null) { }
}

public record Message<TResponse>(string MessageType, Guid AggregateId, MessageSource MessageSource)
    : IRequest<TResponse>
{
    public Message()
        : this(string.Empty, Guid.Empty, null)
    {
        MessageType = GetType().Name;
        try
        {
            var principal = EngineContext.Current.PrincipalAccessor.Principal;
            var ipAddress = EngineContext.Current is { HttpContext: { } }
                ? EngineContext.Current.HttpContext?.Request.Headers["X-Forwarded-For"].ToString()
                : "localhost";

            MessageSource = new MessageSource(
                principal.GetUserName(),
                ipAddress,
                principal.GetUserId(),
                principal.GetTenantId(),
                principal.GetTenantName()
            );
        }
        catch (Exception exception)
        {
            // 构造处于业务热路径，任何情况下不得抛出；先尝试安全记录日志，失败时同样忽略
            try
            {
                EngineContext
                    .Current.Resolve<ILogger<Message<TResponse>>>()
                    ?.LogWarning(exception, "构造 Message 消息来源失败：{MessageType}", GetType().Name);
            }
            catch
            {
                // 记录日志本身失败时忽略，确保构造不抛出
            }
        }
    }
}
